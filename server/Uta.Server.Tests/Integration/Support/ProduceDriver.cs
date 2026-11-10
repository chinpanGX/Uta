using System.Text;
using Grpc.Core;
using MagicOnion;
using Uta.Shared.Contracts.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using VolTech.Server.Abstractions;
using Xunit;

namespace Uta.Server.Tests.Integration.Support;

/// <summary>応答の Phase を見て、次の RPC を決めて呼ぶ自動プレイ。</summary>
public sealed class ProduceDriver(PlayerClient player, bool skipOnly = false, bool preferSupply = false)
{
    private readonly string idPrefix = Guid.NewGuid().ToString("N")[..12];   // ドライバーを作り直しても x-request-id が重ならない
    private readonly List<ExamResultDto> examResults = [];
    private int counter;

    /// <summary>受け付けられた操作の数（失敗した UseCard は数えない）</summary>
    public int Operations { get; private set; }

    /// <summary>応答に ExamResult が入っていたもの、順に</summary>
    public IReadOnlyList<ExamResultDto> ExamResults => examResults;

    private string NextId() => $"{idPrefix}-{++counter}";

    private Task<ProduceStateResponse> CallAsync(Func<IProduceService, UnaryResult<ProduceStateResponse>> call) =>
        player.CallAsync(call, NextId());

    private ProduceStateResponse Accept(ProduceStateResponse response)
    {
        Operations++;
        if (response.ExamResult != null)
        {
            examResults.Add(response.ExamResult);
        }

        return response;
    }

    /// <summary>次の 1 操作を呼び、受け付けられた応答を返す。</summary>
    public async Task<ProduceStateResponse> StepAsync(ProduceStateResponse current)
    {
        var op = current.OperationNumber;
        switch (current.Phase)
        {
            case ProducePhase.WeekSelect:
            {
                var (slot, option, param) = ChooseWeek(current);
                return Accept(await CallAsync(x => x.ChooseWeekActionAsync(new ChooseWeekActionRequest(op, slot, option, param))));
            }

            case ProducePhase.CardOffer:
                return Accept(await CallAsync(x => x.ResolveCardOfferAsync(new ResolveCardOfferRequest(op, CardOfferActionKind.Pick, 0))));

            case ProducePhase.Lesson:
            case ProducePhase.Exam:
            {
                if (!skipOnly)
                {
                    Assert.Equal(current.Lesson.Hand.Length, current.Lesson.UsableHand.Length);
                    for (var i = 0; i < current.Lesson.Hand.Length; i++)
                    {
                        var instanceId = current.Lesson.Hand[i];
                        try
                        {
                            var accepted = Accept(await CallAsync(x => x.ExecuteLessonActionAsync(
                                new ExecuteLessonActionRequest(op, LessonActionKind.UseCard, instanceId, 0))));
                            Assert.True(current.Lesson.UsableHand[i]);
                            return accepted;
                        }
                        catch (RpcException e) when (HasReason(e, "PRODUCE_INVALID_ARGUMENT"))
                        {
                            // 使えないカード。次の手札へ
                            Assert.False(current.Lesson.UsableHand[i]);
                        }
                    }
                }

                return Accept(await CallAsync(x => x.ExecuteLessonActionAsync(new ExecuteLessonActionRequest(op, LessonActionKind.Skip, 0, 0))));
            }

            case ProducePhase.ExamResult:
                return Accept(await CallAsync(x => x.DecideExamRetryAsync(new DecideExamRetryRequest(op, false))));

            default:
                throw new InvalidOperationException($"cannot step at phase {current.Phase}");
        }
    }

    /// <summary>Phase が Finished になるまで続ける。maxOperations を超えたら InvalidOperationException。</summary>
    public async Task<ProduceStateResponse> PlayToEndAsync(ProduceStateResponse current, int maxOperations = 3000)
    {
        while (current.Phase != ProducePhase.Finished)
        {
            if (Operations >= maxOperations)
            {
                throw new InvalidOperationException($"operations exceeded {maxOperations}");
            }

            var next = await StepAsync(current);
            AssertResponse(current, next);
            current = next;
        }

        return current;
    }

    private static void AssertResponse(ProduceStateResponse previous, ProduceStateResponse next)
    {
        Assert.Equal(previous.OperationNumber + 1, next.OperationNumber);
        switch (next.Phase)
        {
            case ProducePhase.Exam:
                Assert.NotNull(next.Lesson);
                Assert.Equal(next.Lesson.MaxTurns, next.Lesson.ExamTurns.Length);
                Assert.Equal(5, next.Lesson.NpcScores.Length);
                break;
            case ProducePhase.Lesson:
                Assert.NotNull(next.Lesson);
                Assert.Empty(next.Lesson.ExamTurns);
                Assert.Empty(next.Lesson.NpcScores);
                break;
        }

        if (next.Phase == ProducePhase.Finished)
        {
            Assert.NotNull(next.Result);
        }
        else
        {
            Assert.Null(next.Result);
        }
    }

    private (int Slot, int Option, ParamKind Param) ChooseWeek(ProduceStateResponse current)
    {
        if (preferSupply)
        {
            var supply = current.WeekActions.FirstOrDefault(x => x.Selectable && x.Kind == WeekChoiceKind.Supply);
            if (supply != null)
            {
                return (supply.SlotIndex, -1, ParamKind.Nothing);
            }
        }

        var lesson = current.WeekActions.FirstOrDefault(x => x.Selectable && x.Kind == WeekChoiceKind.Lesson);
        if (lesson != null)
        {
            return (lesson.SlotIndex, -1, ParamKind.Singing);
        }

        var option = current.ClassOptions.FirstOrDefault(x => x.Selectable);
        var classSlot = current.WeekActions.FirstOrDefault(x => x.Selectable && x.Kind == WeekChoiceKind.Class);
        if (classSlot != null && option != null)
        {
            return (classSlot.SlotIndex, option.Index, ParamKind.Nothing);
        }

        var other = current.WeekActions.FirstOrDefault(x => x.Selectable)
            ?? throw new InvalidOperationException("no selectable week action");
        return (other.SlotIndex, -1, ParamKind.Nothing);
    }

    private static bool HasReason(RpcException e, string reason)
    {
        if (e.StatusCode != StatusCode.FailedPrecondition)
        {
            return false;
        }

        foreach (var entry in e.Trailers)
        {
            if (entry.Key == ApiHeaders.ErrorDetailsTrailer && entry.IsBinary)
            {
                return Encoding.UTF8.GetString(entry.ValueBytes).Contains(reason);
            }
        }

        return false;
    }
}

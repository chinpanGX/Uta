using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

/// <summary>RPC のレスポンスを作るための材料。State が null は「進行中のセッション無し」。</summary>
public sealed record ProduceStateResult(
    ProduceState? State, IReadOnlyList<WeekChoice> WeekChoices, IReadOnlyList<ClassOptionView> ClassOptions,
    IReadOnlyList<int> NpcScores, ExamReport? ExamResult, ProduceEndResult? End)
{
    public static readonly ProduceStateResult None = new(null, [], [], [], null, null);
}

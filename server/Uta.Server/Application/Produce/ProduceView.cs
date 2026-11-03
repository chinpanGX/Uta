using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public sealed record ProduceEndSummary(
    ProduceSessionStatus Ending, ExamOutcome ExamOutcome, int EvaluationValue, ProduceRank Rank, int FinalScore, int FinalRank,
    int GachaCurrency, int Gold, int SupportPoints, int Exp, int PlayerLevel, int PlayerExp);

/// <summary>RPC のレスポンスを作るための材料。State が null は「進行中のセッション無し」。</summary>
public sealed record ProduceView(
    ProduceState? State, IReadOnlyList<WeekChoice> WeekChoices, IReadOnlyList<ClassOptionView> ClassOptions,
    IReadOnlyList<int> NpcScores, ExamReport? ExamResult, ProduceEndSummary? End)
{
    public static readonly ProduceView None = new(null, [], [], [], null, null);
}

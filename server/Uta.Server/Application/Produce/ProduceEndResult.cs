using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public sealed record ProduceEndResult(
    ProduceSessionStatus Ending, ExamResultKind ExamResultKind, int EvaluationValue, ProduceRank Rank, int FinalScore, int FinalRank,
    int GachaCurrency, int Gold, int SupportPoints, int Exp, int PlayerExp);

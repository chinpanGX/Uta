using Uta.Shared.Domain.Produce;

namespace Uta.Server.Domain.Produce;

/// <summary>state 列に入れるもの。マスターシード、乱数ストリームの状態、状態の写し。</summary>
public sealed record ProduceSessionData(ulong MasterSeed, IReadOnlyList<RandomStreamState> Streams, ProduceSnapshot Snapshot);

/// <summary>Data は state_version が今と違うとき・state が読めないとき null（読まない）。</summary>
public sealed record ProduceSessionRecord(Ulid SessionId, int AppVersion, int StateVersion, int RuleVersion, int Version, ProduceSessionData? Data);

public sealed record ProduceResultRecord(
    Ulid SessionId, Ulid PlayerId, ProduceSessionStatus Ending, ExamOutcome ExamOutcome, int EvaluationValue, ProduceRank Rank,
    int FinalScore, int FinalRank, int GachaCurrency, int Gold, int SupportPoints, int Exp)
{
    /// <summary>放棄・破棄の行（数値は 0、ExamOutcome / Rank は Nothing）。</summary>
    public static ProduceResultRecord Empty(Ulid sessionId, Ulid playerId, ProduceSessionStatus ending) =>
        new(sessionId, playerId, ending, ExamOutcome.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0);
}

namespace Uta.Server.Domain.Produce;

/// <summary>produce_sessions.status。1〜3 は produce_results.ending と同じ値。</summary>
public enum ProduceSessionStatus
{
    InProgress = 0,
    Completed = 1,
    Abandoned = 2,
    Discarded = 3,
}

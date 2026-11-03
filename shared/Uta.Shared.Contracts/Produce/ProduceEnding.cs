namespace Uta.Shared.Contracts.Produce
{
    /// <summary>プロデュースの終わり方。ProduceSessionStatus の 1〜3 と同じ値。</summary>
    public enum ProduceEnding
    {
        Nothing = 0,
        Completed = 1,
        Abandoned = 2,
        Discarded = 3,
    }
}

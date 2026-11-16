namespace Uta.Client.Application.Produce
{
    /// <summary>プロデュースの終わり方。Contracts.Produce.ProduceEnding と同じ数値。</summary>
    public enum ProduceEndKind
    {
        Nothing = 0,
        Completed = 1,
        Abandoned = 2,
        Discarded = 3,
    }
}

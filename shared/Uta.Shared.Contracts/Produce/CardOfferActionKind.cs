namespace Uta.Shared.Contracts.Produce
{
    /// <summary>カードの選択肢の局面での操作の種類。</summary>
    public enum CardOfferActionKind
    {
        Nothing = 0,
        Pick = 1,
        Reroll = 2,
        Remove = 3,
        Decline = 4,
    }
}

namespace Uta.Client.Application.Support
{
    /// <summary>サポートカードのレベル上げ・上限解放の結果の種類。</summary>
    public enum SupportResultKind
    {
        Succeeded,
        NotEnoughPoints,
        NotEnoughGold,
        InvalidTarget,
        MaxLimitBreak,
        InvalidMaterial,
        NotOwned,
        Failed,
    }
}

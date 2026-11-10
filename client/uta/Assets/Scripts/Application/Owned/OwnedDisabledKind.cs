namespace Uta.Client.Application.Owned
{
    /// <summary>選択モードで選べない理由。</summary>
    public enum OwnedDisabledKind
    {
        None,
        AlreadyInFormation,
        SameCardInFormation,
        PlanMismatch,
    }
}

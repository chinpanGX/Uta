namespace Uta.Server.Application.Produce;

public static class ProduceVersions
{
    /// <summary>state 列の形を変えたら上げる。</summary>
    public const int StateVersion = 1;

    /// <summary>Uta.Shared.Domain・マスタ・サーバーのルールを変えたコミットで上げる。</summary>
    public const int RuleVersion = 1;

    /// <summary>Data == null（state_version が違う・state が読めない）も古いセッション扱い。</summary>
    public static bool IsCurrent(ProduceSessionRecord session, int appVersion) =>
        session.Data != null && session.AppVersion == appVersion && session.StateVersion == StateVersion && session.RuleVersion == RuleVersion;
}

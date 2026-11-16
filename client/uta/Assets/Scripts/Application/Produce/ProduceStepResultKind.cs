namespace Uta.Client.Application.Produce
{
    public enum ProduceStepResultKind
    {
        /// <summary>session.Current が次の局面になった。</summary>
        Succeeded,

        /// <summary>食い違い（操作番号・局面）だったので GetState で取り直した。session.Current は最新。画面は Phase から選び直す。</summary>
        Recovered,

        /// <summary>セッションが無い・破棄された。session は Clear 済みで、所持品は取り直した（取り直しの失敗は無視）。ホームへ戻る。</summary>
        SessionEnded,

        /// <summary>PRODUCE_INVALID_ARGUMENT。</summary>
        Rejected,

        /// <summary>それ以外（ApiCore の共通ダイアログが出ている）。</summary>
        Failed,
    }
}

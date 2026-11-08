namespace Uta.Client.Application.Player
{
    /// <summary>破棄の知らせを出したか（UI の状態。エンティティに入れない。ADR 0021）。書くのは PlayerUseCase だけ。</summary>
    public sealed class DiscardNoticeState
    {
        public bool Shown { get; private set; }

        public void MarkShown()
        {
            Shown = true;
        }

        public void Reset()
        {
            Shown = false;
        }
    }
}

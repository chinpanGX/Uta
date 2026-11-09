namespace Uta.Client.Presentation.Owned
{
    public sealed class SupportLevelStepViewDto
    {
        public SupportLevelStepViewDto(string label, bool enabled, int targetLevel, string confirmText)
        {
            Label = label;
            Enabled = enabled;
            TargetLevel = targetLevel;
            ConfirmText = confirmText;
        }

        public string Label { get; }
        public bool Enabled { get; }

        /// <summary>Enabled のとき目標のレベル。無効なら今のレベル（カードが無ければ 0）。</summary>
        public int TargetLevel { get; }

        /// <summary>Enabled のときの確認の文。無効なら null。</summary>
        public string ConfirmText { get; }
    }
}

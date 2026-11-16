namespace Uta.Client.Presentation.Formation
{
    /// <summary>編成のサポートカードの枠 1 行の表示の値。filled は枠が埋まっているか（外すボタンを押せるか）。</summary>
    public sealed class FormationSlotRowViewDto
    {
        public FormationSlotRowViewDto(string label, bool filled)
        {
            Label = label;
            Filled = filled;
        }

        public string Label { get; }
        public bool Filled { get; }
    }
}

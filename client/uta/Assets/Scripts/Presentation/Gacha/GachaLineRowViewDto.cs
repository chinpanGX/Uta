namespace Uta.Client.Presentation.Gacha
{
    /// <summary>ガチャの 1 行（排出率・結果のモーダル）の表示の値。</summary>
    public sealed class GachaLineRowViewDto
    {
        public GachaLineRowViewDto(string text, bool isHeading)
        {
            Text = text;
            IsHeading = isHeading;
        }

        public string Text { get; }

        /// <summary>排出率のモーダルだけが見る（結果のモーダルは常に false）。</summary>
        public bool IsHeading { get; }
    }
}

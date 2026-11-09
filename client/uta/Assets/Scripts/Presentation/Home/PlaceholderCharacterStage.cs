using System;
using TMPro;

namespace Uta.Client.Presentation.Home
{
    /// <summary>MVP の仮の枠。枠の文字に育成アイドルの名前を出すだけ。RawImage には触らない。</summary>
    public sealed class PlaceholderCharacterStage : ICharacterStage
    {
        public const string EmptyText = "キャラ表示";

        private readonly TMP_Text label;

        /// <summary>label が null なら ArgumentNullException（引数名 "label"）。</summary>
        public PlaceholderCharacterStage(TMP_Text label)
        {
            this.label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public void ShowIdol(int idolId, string name)
        {
            label.text = name;
        }

        public void Clear()
        {
            label.text = EmptyText;
        }
    }
}

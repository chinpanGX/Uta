using System;
using System.Globalization;
using Uta.MasterData;

namespace Uta.Client.Presentation.Gacha
{
    public static class GachaTexts
    {
        /// <summary>Idol → "育成アイドル"、SupportCard → "サポートカード"、それ以外 → ArgumentOutOfRangeException。</summary>
        public static string Kind(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Idol:
                    return "育成アイドル";
                case ItemKind.SupportCard:
                    return "サポートカード";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>0.01% 単位の値を "12.34%" の形にする。負なら ArgumentOutOfRangeException。</summary>
        public static string Percent(int basisPoints)
        {
            if (basisPoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basisPoints));
            }

            return (basisPoints / 100).ToString(CultureInfo.InvariantCulture)
                + "."
                + (basisPoints % 100).ToString("00", CultureInfo.InvariantCulture)
                + "%";
        }
    }
}

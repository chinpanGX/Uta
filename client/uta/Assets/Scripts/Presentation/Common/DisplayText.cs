using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Presentation.Common
{
    /// <summary>画面に出す名前の文字（状態を持たない）。メソッド名を型と同じにしない（クラスの中の単純名がメソッドに束縛されるため）。</summary>
    public static class DisplayText
    {
        public static string RarityName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.N: return "N";
                case Rarity.R: return "R";
                case Rarity.SR: return "SR";
                case Rarity.SSR: return "SSR";
                default: return "-";
            }
        }

        public static string PlanName(Plan plan)
        {
            switch (plan)
            {
                case Plan.Free: return "共通";
                case Plan.Sense: return "感性";
                case Plan.Logic: return "理論";
                default: return "-";
            }
        }

        public static string SupportTypeName(SupportType type)
        {
            switch (type)
            {
                case SupportType.Singing: return "歌唱";
                case SupportType.Rhythm: return "律動";
                case SupportType.Expression: return "表現";
                case SupportType.Assist: return "アシスト";
                default: return "-";
            }
        }

        public static string ParamName(ParamKind param)
        {
            switch (param)
            {
                case ParamKind.Singing: return "歌唱";
                case ParamKind.Rhythm: return "律動";
                case ParamKind.Expression: return "表現";
                default: return "-";
            }
        }

        /// <summary>アビリティ（枠 1）の 1 行の文字。value は今のレベルの値。ParamBonus は千分率（float を使わない）。</summary>
        public static string Ability(SupportAbilityKind kind, ParamKind param, int value)
        {
            switch (kind)
            {
                case SupportAbilityKind.InitialParam:
                    return "初期" + ParamName(param) + " +" + value;
                case SupportAbilityKind.ParamBonus:
                    return ParamName(param) + "ボーナス +" + (value / 10) + "." + (value % 10) + "%";
                case SupportAbilityKind.MaxHp:
                    return "最大体力 +" + value;
                default:
                    return "-";
            }
        }
    }
}

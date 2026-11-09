using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Owned
{
    /// <summary>所持一覧の 1 件。表示の文字列は持たない（文字は Presentation の OwnedDtoFactory が作る）。</summary>
    public sealed class OwnedListItem
    {
        public OwnedListItem(bool isIdol, int idolId, string supportCardUid, int cardId, string name, Rarity rarity, Plan plan,
            SupportType supportType, int level, int limitBreak, int shards, bool enabled, OwnedDisabledKind disabledKind)
        {
            IsIdol = isIdol;
            IdolId = idolId;
            SupportCardUid = supportCardUid;
            CardId = cardId;
            Name = name;
            Rarity = rarity;
            Plan = plan;
            SupportType = supportType;
            Level = level;
            LimitBreak = limitBreak;
            Shards = shards;
            Enabled = enabled;
            DisabledKind = disabledKind;
        }

        public bool IsIdol { get; }

        /// <summary>育成アイドル: IdolId。サポートカード: 0。</summary>
        public int IdolId { get; }

        /// <summary>サポートカード: 所持ID。育成アイドル: null。</summary>
        public string SupportCardUid { get; }

        /// <summary>サポートカード: マスタのサポートカード ID。育成アイドル: 0。</summary>
        public int CardId { get; }

        public string Name { get; }
        public Rarity Rarity { get; }
        public Plan Plan { get; }

        /// <summary>育成アイドルは SupportType.Nothing。</summary>
        public SupportType SupportType { get; }

        /// <summary>育成アイドルは 0。</summary>
        public int Level { get; }

        /// <summary>育成アイドルは 0。</summary>
        public int LimitBreak { get; }

        /// <summary>育成アイドルのかけら。サポートカードは 0。</summary>
        public int Shards { get; }

        /// <summary>選択モードで選べるか（閲覧モードは常に true）。</summary>
        public bool Enabled { get; }

        /// <summary>Enabled なら None。</summary>
        public OwnedDisabledKind DisabledKind { get; }
    }
}

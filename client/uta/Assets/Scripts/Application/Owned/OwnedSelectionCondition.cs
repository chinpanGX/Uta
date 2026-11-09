using System;
using System.Collections.Generic;

namespace Uta.Client.Application.Owned
{
    /// <summary>選択モードの条件（値だけ。完了の仕組みは持たない）。</summary>
    public sealed class OwnedSelectionCondition
    {
        private OwnedSelectionCondition(OwnedSelectionKind kind, IReadOnlyCollection<int> excludedIdolIds, int idolId,
            IReadOnlyCollection<string> excludedUids, int materialCardId, string materialTargetUid)
        {
            Kind = kind;
            ExcludedIdolIds = excludedIdolIds ?? Array.Empty<int>();
            IdolId = idolId;
            ExcludedUids = excludedUids ?? Array.Empty<string>();
            MaterialCardId = materialCardId;
            MaterialTargetUid = materialTargetUid;
        }

        /// <summary>育成アイドルを選ばせる。excludedIdolIds は選べない（AlreadyInFormation）。null は空。</summary>
        public static OwnedSelectionCondition ForIdol(IReadOnlyCollection<int> excludedIdolIds = null)
        {
            return new OwnedSelectionCondition(OwnedSelectionKind.Idol, excludedIdolIds, 0, null, 0, null);
        }

        /// <summary>編成の枠に入れるサポートカードを選ばせる。idolId は育成アイドルの IdolId（0 ならプランで弾かない）。excludedUids はほかの枠の所持ID（null は空）。</summary>
        public static OwnedSelectionCondition ForSupportCard(int idolId, IReadOnlyCollection<string> excludedUids)
        {
            return new OwnedSelectionCondition(OwnedSelectionKind.SupportCard, null, idolId, excludedUids, 0, null);
        }

        /// <summary>上限解放の素材を選ばせる。MasterId == supportCardId で targetUid 以外の所持だけを並べる（全部選べる）。targetUid が null・空なら ArgumentException。</summary>
        public static OwnedSelectionCondition ForLimitBreakMaterial(int supportCardId, string targetUid)
        {
            if (string.IsNullOrEmpty(targetUid))
            {
                throw new ArgumentException("targetUid is null or empty.", nameof(targetUid));
            }

            return new OwnedSelectionCondition(OwnedSelectionKind.LimitBreakMaterial, null, 0, null, supportCardId, targetUid);
        }

        public OwnedSelectionKind Kind { get; }

        /// <summary>Idol 以外は空。</summary>
        public IReadOnlyCollection<int> ExcludedIdolIds { get; }

        /// <summary>SupportCard 以外は 0。</summary>
        public int IdolId { get; }

        /// <summary>SupportCard 以外は空。</summary>
        public IReadOnlyCollection<string> ExcludedUids { get; }

        /// <summary>LimitBreakMaterial 以外は 0。</summary>
        public int MaterialCardId { get; }

        /// <summary>LimitBreakMaterial 以外は null。</summary>
        public string MaterialTargetUid { get; }
    }
}

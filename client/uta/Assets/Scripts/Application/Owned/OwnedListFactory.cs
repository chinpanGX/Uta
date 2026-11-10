using System;
using System.Collections.Generic;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.SupportCard;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Owned
{
    /// <summary>所持一覧の並び・絞り込み・選べるか（外部に触れない）。マスタの型は Uta.MasterData. 付きで書く。</summary>
    public sealed class OwnedListFactory
    {
        private readonly IReadOnlyIdolRepository idolRepository;
        private readonly IReadOnlySupportCardRepository supportCardRepository;
        private readonly Uta.MasterData.MemoryDatabase memoryDatabase;

        public OwnedListFactory(IReadOnlyIdolRepository idolRepository, IReadOnlySupportCardRepository supportCardRepository, Uta.MasterData.MemoryDatabase memoryDatabase)
        {
            this.idolRepository = idolRepository;
            this.supportCardRepository = supportCardRepository;
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>
        /// 所持の育成アイドルを IdolId の昇順に並べた一覧（並び替え・絞り込みは無い）。condition が null なら全部 Enabled。
        /// Kind == Idol なら ExcludedIdolIds の行を Enabled = false・AlreadyInFormation。ほかの Kind は ArgumentException。
        /// </summary>
        public IReadOnlyList<OwnedListItem> CreateIdols(OwnedSelectionCondition condition)
        {
            if (condition != null && condition.Kind != OwnedSelectionKind.Idol)
            {
                throw new ArgumentException("condition.Kind must be Idol.", nameof(condition));
            }

            var owned = new List<IdolEntity>(idolRepository.FindAll());
            owned.Sort((a, b) => a.MasterId.CompareTo(b.MasterId));
            var items = new List<OwnedListItem>(owned.Count);
            foreach (var entity in owned)
            {
                var row = memoryDatabase.IdolTable.FindByIdolId(entity.MasterId);
                var excluded = condition != null && Contains(condition.ExcludedIdolIds, entity.MasterId);
                items.Add(new OwnedListItem(true, entity.MasterId, null, 0, row.Name, (Rarity)(int)row.Rarity, (Plan)(int)row.Plan,
                    SupportType.Nothing, 0, 0, entity.Shards, !excluded,
                    excluded ? OwnedDisabledKind.AlreadyInFormation : OwnedDisabledKind.None));
            }

            return items;
        }

        /// <summary>
        /// 手順: (1) LimitBreakMaterial なら MasterId == MaterialCardId かつ UniqueId != MaterialTargetUid の所持だけ残す。
        /// (2) planFilter が null でなければ Plan が完全一致のものだけ。(3) supportTypeFilter が null でなければ Type が一致のものだけ。
        /// (4) 並べる（第 1 の基準が同じなら所持IDの昇順。descending は第 1 の基準だけ逆）。(5) 選べるかを決める。
        /// condition が null・LimitBreakMaterial なら全部 Enabled。Kind == Idol なら ArgumentException。
        /// </summary>
        public IReadOnlyList<OwnedListItem> CreateSupportCards(OwnedSortKind sort, bool descending, Plan? planFilter, SupportType? supportTypeFilter, OwnedSelectionCondition condition)
        {
            if (condition != null && condition.Kind == OwnedSelectionKind.Idol)
            {
                throw new ArgumentException("condition.Kind must not be Idol.", nameof(condition));
            }

            var entries = new List<Entry>();
            foreach (var entity in supportCardRepository.FindAll())
            {
                if (condition != null && condition.Kind == OwnedSelectionKind.LimitBreakMaterial
                    && !IsLimitBreakMaterial(entity, condition.MaterialCardId, condition.MaterialTargetUid))
                {
                    continue;
                }

                var row = memoryDatabase.SupportCardTable.FindBySupportCardId(entity.MasterId);
                var plan = (Plan)(int)row.Plan;
                var type = (SupportType)(int)row.Type;
                if (planFilter.HasValue && plan != planFilter.Value)
                {
                    continue;
                }

                if (supportTypeFilter.HasValue && type != supportTypeFilter.Value)
                {
                    continue;
                }

                entries.Add(new Entry(entity, row.Name, (Rarity)(int)row.Rarity, plan, type));
            }

            entries.Sort((a, b) =>
            {
                var first = CompareFirst(sort, a, b);
                if (descending)
                {
                    first = -first;
                }

                return first != 0 ? first : string.CompareOrdinal(a.Entity.UniqueId, b.Entity.UniqueId);
            });

            var excludedCardIds = new HashSet<int>();
            var idolPlan = Plan.Nothing;
            var selectingSupport = condition != null && condition.Kind == OwnedSelectionKind.SupportCard;
            if (selectingSupport)
            {
                foreach (var uid in condition.ExcludedUids)
                {
                    var found = supportCardRepository.Find(uid);
                    if (found != null)
                    {
                        excludedCardIds.Add(found.MasterId);
                    }
                }

                if (condition.IdolId != 0)
                {
                    idolPlan = (Plan)(int)memoryDatabase.IdolTable.FindByIdolId(condition.IdolId).Plan;
                }
            }

            var items = new List<OwnedListItem>(entries.Count);
            foreach (var entry in entries)
            {
                var kind = OwnedDisabledKind.None;
                if (selectingSupport)
                {
                    if (Contains(condition.ExcludedUids, entry.Entity.UniqueId))
                    {
                        kind = OwnedDisabledKind.AlreadyInFormation;
                    }
                    else if (excludedCardIds.Contains(entry.Entity.MasterId))
                    {
                        kind = OwnedDisabledKind.SameCardInFormation;
                    }
                    else if (condition.IdolId != 0 && !FormationValidator.IsPlanCompatible(idolPlan, entry.Plan))
                    {
                        kind = OwnedDisabledKind.PlanMismatch;
                    }
                }

                items.Add(new OwnedListItem(false, 0, entry.Entity.UniqueId, entry.Entity.MasterId, entry.Name, entry.Rarity, entry.Plan,
                    entry.SupportType, entry.Entity.Level, entry.Entity.LimitBreak, 0, kind == OwnedDisabledKind.None, kind));
            }

            return items;
        }

        /// <summary>上限解放の素材にできる所持（MasterId == supportCardId かつ UniqueId != targetUid）が 1 つ以上あるか。</summary>
        public bool HasLimitBreakMaterial(int supportCardId, string targetUid)
        {
            foreach (var entity in supportCardRepository.FindAll())
            {
                if (IsLimitBreakMaterial(entity, supportCardId, targetUid))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLimitBreakMaterial(SupportCardEntity entity, int supportCardId, string targetUid)
        {
            return entity.MasterId == supportCardId && entity.UniqueId != targetUid;
        }

        private static int CompareFirst(OwnedSortKind sort, Entry a, Entry b)
        {
            switch (sort)
            {
                case OwnedSortKind.Rarity:
                    return ((int)a.Rarity).CompareTo((int)b.Rarity);
                case OwnedSortKind.Level:
                    return a.Entity.Level.CompareTo(b.Entity.Level);
                default:
                    return string.CompareOrdinal(a.Entity.UniqueId, b.Entity.UniqueId);
            }
        }

        private static bool Contains<T>(IReadOnlyCollection<T> collection, T value)
        {
            foreach (var item in collection)
            {
                if (EqualityComparer<T>.Default.Equals(item, value))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class Entry
        {
            public Entry(SupportCardEntity entity, string name, Rarity rarity, Plan plan, SupportType supportType)
            {
                Entity = entity;
                Name = name;
                Rarity = rarity;
                Plan = plan;
                SupportType = supportType;
            }

            public SupportCardEntity Entity { get; }
            public string Name { get; }
            public Rarity Rarity { get; }
            public Plan Plan { get; }
            public SupportType SupportType { get; }
        }
    }
}

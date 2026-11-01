using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Uta.MasterData;
using DomainLesson = Uta.Shared.Domain.Lesson;

namespace Uta.Client.Infrastructure
{
    /// <summary>マスタの行を Shared.Domain の行の型に詰めて CardCatalog を作る。グループの解決と組み立ては CardCatalog.Create が行う（repository-layout.md 4章）。</summary>
    public static class MasterCardCatalog
    {
        public static DomainLesson.CardCatalog Create(MemoryDatabase db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var cards = db.CardTable.All
                .Select(x => new DomainLesson.CardRow(
                    x.CardId,
                    (DomainLesson.Plan)(int)x.Plan,
                    (DomainLesson.CardType)(int)x.Type,
                    (DomainLesson.Rarity)(int)x.Rarity,
                    x.UnlockLv,
                    x.OnceInLesson,
                    x.Unique,
                    x.StartInHand))
                .ToList();
            var faces = db.CardFaceTable.All
                .Select(x => new DomainLesson.CardFaceRow(x.CardId, (DomainLesson.CardFaceKind)(int)x.Face, x.EffectGroupId, x.UseConditionGroupId))
                .ToList();
            var costs = db.CardCostTable.All
                .Select(x => new DomainLesson.CardCostRow(x.CardId, (DomainLesson.CardFaceKind)(int)x.Face, x.Order, (DomainLesson.CostKind)(int)x.Kind, x.Value))
                .ToList();
            var effects = db.EffectTable.All
                .Select(x => new DomainLesson.EffectRow(
                    x.EffectGroupId,
                    x.Order,
                    (DomainLesson.EffectOp)(int)x.Op,
                    (DomainLesson.StatusKind)(int)x.Status,
                    (DomainLesson.PersistentTrigger)(int)x.Trigger,
                    x.Value,
                    x.Arg1,
                    x.Arg2,
                    x.Arg3,
                    x.RefConditionGroupId,
                    x.RefEffectGroupId,
                    x.RefCardId))
                .ToList();
            var conditions = db.ConditionTable.All
                .Select(x => new DomainLesson.ConditionRow(
                    x.ConditionGroupId,
                    x.Order,
                    (DomainLesson.ConditionSubject)(int)x.Subject,
                    (DomainLesson.Comparator)(int)x.Comparator,
                    x.Value))
                .ToList();
            return DomainLesson.CardCatalog.Create(cards, faces, costs, effects, conditions);
        }

        public static async UniTask<DomainLesson.CardCatalog> LoadFromStreamingAssetsAsync(CancellationToken cancellationToken = default)
        {
            var db = await MasterDataLoader.LoadFromStreamingAssetsAsync(cancellationToken);
            return Create(db);
        }
    }
}

using Uta.Shared.Domain.Lesson;

namespace Uta.Server.Infrastructure;

/// <summary>
/// マスタの行を Shared.Domain の行の型に詰めて CardCatalog を作る。
/// グループの解決と組み立ては CardCatalog.Create が行う（repository-layout.md 4章）。
/// </summary>
public static class MasterCardCatalog
{
    public static CardCatalog Create(Uta.MasterData.MemoryDatabase db)
    {
        var cards = db.CardTable.All
            .Select(x => new CardRow(
                x.CardId,
                (Plan)(int)x.Plan,
                (CardType)(int)x.Type,
                (Rarity)(int)x.Rarity,
                x.UnlockLv,
                x.OnceInLesson,
                x.Unique,
                x.StartInHand))
            .ToList();
        var faces = db.CardFaceTable.All
            .Select(x => new CardFaceRow(x.CardId, (CardFaceKind)(int)x.Face, x.EffectGroupId, x.UseConditionGroupId))
            .ToList();
        var costs = db.CardCostTable.All
            .Select(x => new CardCostRow(x.CardId, (CardFaceKind)(int)x.Face, x.Order, (CostKind)(int)x.Kind, x.Value))
            .ToList();
        var effects = db.EffectTable.All
            .Select(x => new EffectRow(
                x.EffectGroupId,
                x.Order,
                (EffectOp)(int)x.Op,
                (StatusKind)(int)x.Status,
                (PersistentTrigger)(int)x.Trigger,
                x.Value,
                x.Arg1,
                x.Arg2,
                x.Arg3,
                x.RefConditionGroupId,
                x.RefEffectGroupId,
                x.RefCardId))
            .ToList();
        var conditions = db.ConditionTable.All
            .Select(x => new ConditionRow(
                x.ConditionGroupId,
                x.Order,
                (ConditionSubject)(int)x.Subject,
                (Comparator)(int)x.Comparator,
                x.Value))
            .ToList();
        return CardCatalog.Create(cards, faces, costs, effects, conditions);
    }
}

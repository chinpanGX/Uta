using Uta.MasterData;
using Uta.Server.Application.Support;

namespace Uta.Server.Infrastructure;

public sealed class MasterSupportCatalog : ISupportCatalog
{
    private readonly Dictionary<int, Rarity> rarities;
    private readonly Dictionary<(Rarity, int), int> maxLevels;
    private readonly Dictionary<(Rarity, int), LevelUpCost> costs;

    public MasterSupportCatalog(MemoryDatabase db)
    {
        rarities = db.SupportCardTable.All.ToDictionary(x => x.SupportCardId, x => x.Rarity);
        maxLevels = db.SupportLevelCapTable.All.ToDictionary(x => (x.Rarity, x.LimitBreak), x => x.MaxLevel);
        costs = db.SupportLevelCostTable.All.ToDictionary(x => (x.Rarity, x.Level), x => new LevelUpCost(x.SupportPoints, x.Gold));
    }

    public int GetMaxLevel(int cardId, int limitBreak) =>
        maxLevels.TryGetValue((GetRarity(cardId), limitBreak), out var max)
            ? max
            : throw new InvalidOperationException($"support level cap not found: card {cardId} limit break {limitBreak}");

    public LevelUpCost GetLevelUpCost(int cardId, int fromLevel, int toLevel)
    {
        var rarity = GetRarity(cardId);
        var supportPoints = 0;
        var gold = 0;
        for (var level = fromLevel; level < toLevel; level++)
        {
            if (!costs.TryGetValue((rarity, level), out var cost))
            {
                throw new InvalidOperationException($"support level cost not found: {rarity} level {level}");
            }

            supportPoints += cost.SupportPoints;
            gold += cost.Gold;
        }

        return new LevelUpCost(supportPoints, gold);
    }

    private Rarity GetRarity(int cardId) =>
        rarities.TryGetValue(cardId, out var rarity)
            ? rarity
            : throw new InvalidOperationException($"support card not found: {cardId}");
}

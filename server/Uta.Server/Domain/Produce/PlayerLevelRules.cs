namespace Uta.Server.Domain.Produce;

public static class PlayerLevelRules
{
    /// <param name="requiredExps">requiredExps[i] は Lv i+1 → i+2 に要る経験値。最大 Lv は requiredExps.Count + 1</param>
    public static (int Level, int Exp) AddExp(int level, int exp, int gain, IReadOnlyList<int> requiredExps)
    {
        var maxLevel = requiredExps.Count + 1;
        while (gain > 0 && level < maxLevel)
        {
            var remain = requiredExps[level - 1] - exp;
            if (gain >= remain)
            {
                gain -= remain;
                level++;
                exp = 0;
            }
            else
            {
                exp += gain;
                gain = 0;
            }
        }

        return (level, level >= maxLevel ? 0 : exp);
    }
}

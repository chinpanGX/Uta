namespace Uta.Server.Domain.Player;

public static class PlayerExpRules
{
    /// <summary>経験値の累計に gain を足し、maxExp で止める。totalExp が既に maxExp 以上なら totalExp のまま（上限をマスタで下げても減らさない）。</summary>
    /// <exception cref="ArgumentOutOfRangeException">gain が負</exception>
    public static int AddExp(int totalExp, int gain, int maxExp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gain);
        if (totalExp >= maxExp)
        {
            return totalExp;
        }

        return Math.Min(totalExp + gain, maxExp);
    }
}

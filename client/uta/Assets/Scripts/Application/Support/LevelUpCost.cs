namespace Uta.Client.Application.Support
{
    /// <summary>レベル上げの費用の合計。</summary>
    public sealed class LevelUpCost
    {
        public LevelUpCost(int supportPoints, int gold)
        {
            SupportPoints = supportPoints;
            Gold = gold;
        }

        public int SupportPoints { get; }

        public int Gold { get; }
    }
}

namespace Uta.Client.Domain.Player
{
    public sealed class PlayerEntity
    {
        public PlayerEntity(int level, int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce)
        {
            Level = level;
            Exp = exp;
            Gold = gold;
            SupportPoints = supportPoints;
            GachaCurrency = gachaCurrency;
            HasActiveProduce = hasActiveProduce;
            DiscardedProduce = discardedProduce;
        }

        public int Level { get; }
        public int Exp { get; }
        public int Gold { get; }
        public int SupportPoints { get; }
        public int GachaCurrency { get; }
        public bool HasActiveProduce { get; }
        public bool DiscardedProduce { get; }
    }
}

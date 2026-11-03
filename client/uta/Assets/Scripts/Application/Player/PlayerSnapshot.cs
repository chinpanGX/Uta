using System;
using System.Collections.Generic;

namespace Uta.Client.Application.Player
{
    public sealed class PlayerSnapshot
    {
        public PlayerSnapshot(int level, int exp, int gold, int supportPoints, int gachaCurrency,
            bool hasActiveProduce, bool discardedProduce,
            IReadOnlyList<OwnedIdol> idols, IReadOnlyList<OwnedSupportCard> supportCards)
        {
            if (idols == null)
            {
                throw new ArgumentNullException(nameof(idols));
            }

            if (supportCards == null)
            {
                throw new ArgumentNullException(nameof(supportCards));
            }

            Level = level;
            Exp = exp;
            Gold = gold;
            SupportPoints = supportPoints;
            GachaCurrency = gachaCurrency;
            HasActiveProduce = hasActiveProduce;
            DiscardedProduce = discardedProduce;
            Idols = idols;
            SupportCards = supportCards;
        }

        public int Level { get; }
        public int Exp { get; }
        public int Gold { get; }
        public int SupportPoints { get; }
        public int GachaCurrency { get; }
        public bool HasActiveProduce { get; }
        public bool DiscardedProduce { get; }
        public IReadOnlyList<OwnedIdol> Idols { get; }
        public IReadOnlyList<OwnedSupportCard> SupportCards { get; }
    }
}

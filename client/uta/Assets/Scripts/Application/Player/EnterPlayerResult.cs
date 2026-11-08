using Uta.Client.Domain.Player;

namespace Uta.Client.Application.Player
{
    public sealed class EnterPlayerResult
    {
        public EnterPlayerResult(bool signedUp, bool created, PlayerEntity player, int idolCount, int supportCardCount)
        {
            SignedUp = signedUp;
            Created = created;
            Player = player;
            IdolCount = idolCount;
            SupportCardCount = supportCardCount;
        }

        /// <summary>SignUpAsync を呼んだか。</summary>
        public bool SignedUp { get; }

        /// <summary>SignUpAsync が「今回作った」と返したか（呼ばなかったら false）。</summary>
        public bool Created { get; }

        public PlayerEntity Player { get; }
        public int IdolCount { get; }
        public int SupportCardCount { get; }
    }
}

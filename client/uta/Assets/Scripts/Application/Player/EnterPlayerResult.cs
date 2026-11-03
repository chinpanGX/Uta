namespace Uta.Client.Application.Player
{
    public sealed class EnterPlayerResult
    {
        public EnterPlayerResult(bool signedUp, bool created, PlayerSnapshot snapshot)
        {
            SignedUp = signedUp;
            Created = created;
            Snapshot = snapshot;
        }

        /// <summary>SignUpAsync を呼んだか。</summary>
        public bool SignedUp { get; }

        /// <summary>SignUpAsync が「今回作った」と返したか（呼ばなかったら false）。</summary>
        public bool Created { get; }
        public PlayerSnapshot Snapshot { get; }
    }
}

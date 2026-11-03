using System;

namespace Uta.Client.Application.Player
{
    public sealed class PlayerCache
    {
        /// <summary>サインインするまで null。</summary>
        public PlayerSnapshot Current { get; private set; }

        public void Set(PlayerSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Current = snapshot;
        }
    }
}

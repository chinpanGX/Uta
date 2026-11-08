using System;
using Uta.Client.Domain.Player;

namespace Uta.Client.Infrastructure.Player
{
    public sealed class PlayerRepository : IPlayerRepository
    {
        private PlayerEntity current;

        public PlayerEntity Find()
        {
            return current;
        }

        public void Upsert(PlayerEntity player)
        {
            current = player ?? throw new ArgumentNullException(nameof(player));
        }
    }
}

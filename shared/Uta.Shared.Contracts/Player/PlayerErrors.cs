using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Player
{
    public static class PlayerErrors
    {
        public static readonly ErrorCode NotCreated = new ErrorCode("uta", "PLAYER_NOT_CREATED");
        public static readonly ErrorCode NotEnoughGold = new ErrorCode("uta", "PLAYER_NOT_ENOUGH_GOLD");
    }
}

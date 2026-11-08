namespace Uta.Client.Domain.Player
{
    public interface IPlayerRepository : IReadOnlyPlayerRepository
    {
        /// <summary>null なら ArgumentNullException。</summary>
        void Upsert(PlayerEntity player);
    }
}

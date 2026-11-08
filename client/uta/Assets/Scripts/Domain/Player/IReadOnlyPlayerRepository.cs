namespace Uta.Client.Domain.Player
{
    public interface IReadOnlyPlayerRepository
    {
        /// <summary>無ければ null。</summary>
        PlayerEntity Find();
    }
}

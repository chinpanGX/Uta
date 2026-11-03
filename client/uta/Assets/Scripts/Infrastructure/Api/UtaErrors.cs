using Uta.Shared.Contracts.Player;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Api
{
    public static class UtaErrors
    {
        /// <summary>アプリの業務エラーの一覧。呼ぶたびに新しい配列を返す（static の可変な配列を持たない）。</summary>
        public static ErrorDefinition[] Create()
        {
            return new[] { new ErrorDefinition(PlayerErrors.NotCreated, ErrorAction.Title) };
        }
    }
}

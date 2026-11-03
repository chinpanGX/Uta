using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Shared.Contracts.Player;
using VolTech.Client;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Api
{
    public sealed class PlayerApi : ApiServiceBase<IPlayerService>
    {
        public PlayerApi(ApiCore core, IPlayerService client) : base(core, client)
        {
        }

        public UniTask<ApiResult<SignUpResponse>> SignUpAsync()
            => InvokeAsync(nameof(IPlayerService.SignUpAsync), 0, static (c, _) => c.SignUpAsync());

        public UniTask<ApiResult<SignInResponse>> SignInAsync(IEnumerable<ErrorCode> handledCodes = null)
            => InvokeAsync(nameof(IPlayerService.SignInAsync), 0, static (c, _) => c.SignInAsync(), handledCodes);
    }
}

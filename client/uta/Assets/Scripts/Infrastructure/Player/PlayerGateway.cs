using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Player;
using Uta.Client.Infrastructure.Api;
using Uta.Shared.Contracts.Player;
using VolTech.Server.Abstractions;

namespace Uta.Client.Infrastructure.Player
{
    public sealed class PlayerGateway : IPlayerPort
    {
        private readonly PlayerApi player;
        private readonly ErrorCode[] notCreated = { PlayerErrors.NotCreated };

        public PlayerGateway(UtaApiClient api)
        {
            player = api.Player;
        }

        public async UniTask<PlayerSignUpResult> SignUpAsync()
        {
            var result = await player.SignUpAsync();
            return result.IsSuccess ? PlayerSignUpResult.Success(result.Value.Created) : PlayerSignUpResult.Failure(result.Error.ToString());
        }

        public async UniTask<PlayerSignInResult> SignInAsync()
        {
            var result = await player.SignInAsync(notCreated);
            if (result.IsSuccess)
            {
                return PlayerSignInResult.Success(ToSnapshot(result.Value));
            }

            if (result.Error.Code.Equals(PlayerErrors.NotCreated))
            {
                return PlayerSignInResult.NotCreated();
            }

            return PlayerSignInResult.Failure(result.Error.ToString());
        }

        /// <summary>DTO → Application の型。Idols・SupportCards が null なら空として扱う。</summary>
        public static PlayerSnapshot ToSnapshot(SignInResponse response)
        {
            var idols = new List<OwnedIdol>();
            if (response.Idols != null)
            {
                foreach (var dto in response.Idols)
                {
                    idols.Add(new OwnedIdol(dto.IdolId, dto.Training, dto.Awakening, dto.Shards));
                }
            }

            var supportCards = new List<OwnedSupportCard>();
            if (response.SupportCards != null)
            {
                foreach (var dto in response.SupportCards)
                {
                    supportCards.Add(new OwnedSupportCard(dto.SupportCardUid, dto.CardId, dto.Level, dto.LimitBreak));
                }
            }

            return new PlayerSnapshot(
                response.Level, response.Exp, response.Gold, response.SupportPoints, response.GachaCurrency,
                response.HasActiveProduce, response.DiscardedProduce, idols, supportCards);
        }
    }
}

using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Shared.Contracts.Player;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Player
{
    public sealed class PlayerUseCase : IPlayerUseCase
    {
        private readonly IPlayerService player;
        private readonly IPlayerRepository players;
        private readonly IIdolRepository idols;
        private readonly ISupportCardRepository supportCards;
        private readonly DiscardNoticeState discardNoticeState;

        public PlayerUseCase(IPlayerService player, IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, DiscardNoticeState discardNoticeState)
        {
            this.player = player;
            this.players = players;
            this.idols = idols;
            this.supportCards = supportCards;
            this.discardNoticeState = discardNoticeState;
        }

        /// <summary>
        /// afterRegistration が true（端末を登録した直後）なら、先に SignUp を呼ぶ。そのあと SignIn を呼ぶ。
        /// SignIn が未作成を返したら、まだ SignUp を呼んでいなければ SignUp してから SignIn をやり直す（1 回だけ）。
        /// SignUp 済みなのに未作成が返ったら InvalidOperationException。成功したら 3 つのリポジトリに書く。
        /// </summary>
        public async UniTask<EnterPlayerResult> EnterAsync(bool afterRegistration)
        {
            var signedUp = false;
            var created = false;

            if (afterRegistration)
            {
                created = await SignUpOrThrowAsync();
                signedUp = true;
            }

            var signIn = await SignInAsync();
            if (signIn.ErrorCode == PlayerErrors.NotCreated)
            {
                if (signedUp)
                {
                    throw new InvalidOperationException("sign-in: player not created after sign-up");
                }

                created = await SignUpOrThrowAsync();
                signedUp = true;

                signIn = await SignInAsync();
                if (signIn.ErrorCode == PlayerErrors.NotCreated)
                {
                    throw new InvalidOperationException("sign-in: player not created after sign-up");
                }
            }

            if (signIn.IsError)
            {
                throw new InvalidOperationException("sign-in failed: " + signIn.Error);
            }

            var response = signIn.Value;
            Apply(response);
            return new EnterPlayerResult(
                signedUp, created, PlayerMapper.ToPlayerEntity(response), response.Idols?.Length ?? 0, response.SupportCards?.Length ?? 0);
        }

        public async UniTask RefreshAsync()
        {
            var signIn = await SignInAsync();
            if (signIn.ErrorCode == PlayerErrors.NotCreated)
            {
                throw new InvalidOperationException("sign-in failed: player not created");
            }

            if (signIn.IsError)
            {
                throw new InvalidOperationException("sign-in failed: " + signIn.Error);
            }

            Apply(signIn.Value);
        }

        public void MarkDiscardNoticeShown()
        {
            discardNoticeState.MarkShown();
        }

        private async UniTask<bool> SignUpOrThrowAsync()
        {
            var result = await player.SignUpAsync().ToResult();
            if (result.IsError)
            {
                throw new InvalidOperationException("sign-up failed: " + result.Error);
            }

            return result.Value.Created;
        }

        private UniTask<ApiResult<SignInResponse>> SignInAsync()
        {
            return player.WithHandledCodes(PlayerErrors.NotCreated).SignInAsync().ToResult();
        }

        // リポジトリの読み書きはメインスレッドだけ（ADR 0021）。間で await しない
        private void Apply(SignInResponse response)
        {
            // 詰め替え（例外が出うるもの）を先に済ませ、書き込みは続けて行う
            var playerEntity = PlayerMapper.ToPlayerEntity(response);
            var idolEntities = PlayerMapper.ToIdolEntities(response.Idols);
            var supportCardEntities = PlayerMapper.ToSupportCardEntities(response.SupportCards);
            players.Upsert(playerEntity);
            idols.Clear();
            idols.Upsert(idolEntities);
            supportCards.Clear();
            supportCards.Upsert(supportCardEntities);
            discardNoticeState.Reset();
        }
    }
}

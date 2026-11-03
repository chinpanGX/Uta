using System;
using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Player
{
    public sealed class EnterPlayerUseCase
    {
        private readonly IPlayerPort port;
        private readonly PlayerCache cache;

        public EnterPlayerUseCase(IPlayerPort port, PlayerCache cache)
        {
            this.port = port;
            this.cache = cache;
        }

        /// <summary>
        /// afterRegistration が true（端末を登録した直後）なら、先に SignUp を呼ぶ。そのあと SignIn を呼ぶ。
        /// SignIn が PlayerNotCreated を返したら、まだ SignUp を呼んでいなければ SignUp してから SignIn をやり直す（1 回だけ）。
        /// SignUp 済みなのに PlayerNotCreated が返ったら InvalidOperationException。
        /// 成功したら cache.Set(snapshot)。
        /// </summary>
        public async UniTask<EnterPlayerResult> ExecuteAsync(bool afterRegistration)
        {
            var signedUp = false;
            var created = false;

            if (afterRegistration)
            {
                var signUp = await port.SignUpAsync();
                if (!signUp.Succeeded)
                {
                    throw new InvalidOperationException("sign-up failed: " + signUp.ErrorText);
                }

                signedUp = true;
                created = signUp.Created;
            }

            var signIn = await port.SignInAsync();
            if (signIn.Outcome == PlayerSignInOutcome.PlayerNotCreated)
            {
                if (signedUp)
                {
                    throw new InvalidOperationException("sign-in: player not created after sign-up");
                }

                var retrySignUp = await port.SignUpAsync();
                if (!retrySignUp.Succeeded)
                {
                    throw new InvalidOperationException("sign-up failed: " + retrySignUp.ErrorText);
                }

                signedUp = true;
                created = retrySignUp.Created;

                signIn = await port.SignInAsync();
                if (signIn.Outcome == PlayerSignInOutcome.PlayerNotCreated)
                {
                    throw new InvalidOperationException("sign-in: player not created after sign-up");
                }
            }

            if (signIn.Outcome != PlayerSignInOutcome.Succeeded)
            {
                throw new InvalidOperationException("sign-in failed: " + signIn.ErrorText);
            }

            cache.Set(signIn.Snapshot);
            return new EnterPlayerResult(signedUp, created, signIn.Snapshot);
        }
    }
}

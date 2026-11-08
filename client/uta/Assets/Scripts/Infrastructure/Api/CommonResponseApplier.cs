using Cysharp.Threading.Tasks;
using Uta.Client.Infrastructure.Player;
using Uta.Shared.Contracts.Common;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Api
{
    /// <summary>共通レスポンスを受け、項目ごとの Applier に渡す。今は player_diff だけ。</summary>
    public sealed class CommonResponseApplier : ICommonResponseApplier<AppCommonDto>
    {
        private readonly PlayerDiffApplier playerDiffApplier;

        public CommonResponseApplier(PlayerDiffApplier playerDiffApplier)
        {
            this.playerDiffApplier = playerDiffApplier;
        }

        public UniTask ApplyAsync(AppCommonDto common)
        {
            if (common?.PlayerDiff != null)
            {
                playerDiffApplier.Apply(common.PlayerDiff);
            }

            return UniTask.CompletedTask;
        }
    }
}

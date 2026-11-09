using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Gacha;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Player;
using Uta.Shared.Contracts.Gacha;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Gacha
{
    public sealed class GachaUseCase : IGachaUseCase
    {
        private readonly IGachaService gachaService;
        private readonly IReadOnlyPlayerRepository playerRepository;
        private readonly IPlayerUseCase playerUseCase;
        private readonly Uta.MasterData.MemoryDatabase memoryDatabase;

        public GachaUseCase(IGachaService gachaService, IReadOnlyPlayerRepository playerRepository, IPlayerUseCase playerUseCase, Uta.MasterData.MemoryDatabase memoryDatabase)
        {
            this.gachaService = gachaService;
            this.playerRepository = playerRepository;
            this.playerUseCase = playerUseCase;
            this.memoryDatabase = memoryDatabase;
        }

        public async UniTask<int?> GetExchangePointsAsync(int gachaId)
        {
            var result = await gachaService.GetGachasAsync().ToResult();
            if (result.IsError)
            {
                return null;
            }

            return GachaMapper.ToExchangePoints(result.Value, gachaId);
        }

        public async UniTask<DrawResult> DrawAsync(int gachaId, int count)
        {
            if (count != 1 && count != 10)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var master = memoryDatabase.GachaTable.FindByGachaId(gachaId);
            var cost = count == 1 ? master.SingleCost : master.TenCost;

            var player = playerRepository.Find();
            if (player == null || player.GachaCurrency < cost)
            {
                return DrawResult.Rejected(DrawResultKind.NotEnoughCurrency);
            }

            var result = await gachaService.WithHandledCodes(GachaErrors.NotEnoughCurrency)
                .DrawAsync(new DrawRequest(gachaId, count, cost)).ToResult();
            if (result.IsError)
            {
                return result.ErrorCode == GachaErrors.NotEnoughCurrency
                    ? DrawResult.Rejected(DrawResultKind.NotEnoughCurrency)
                    : DrawResult.Rejected(DrawResultKind.Failed);
            }

            // 詰め替えを先に済ませる。player_diff は基盤が返す前にリポジトリへ反映済み
            var drawn = GachaMapper.ToDrawResult(result.Value);
            try
            {
                await playerUseCase.RefreshAsync();
            }
            catch (InvalidOperationException)
            {
                // 取り直しの失敗は成功を妨げない（通信の失敗のダイアログは ApiCore が出している）
            }

            return drawn;
        }
    }
}

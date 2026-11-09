using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Player;
using Uta.Client.Application.Support;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Support;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Support
{
    public sealed class SupportUseCase : ISupportUseCase
    {
        private readonly ISupportService supportService;
        private readonly IPlayerUseCase playerUseCase;

        public SupportUseCase(ISupportService supportService, IPlayerUseCase playerUseCase)
        {
            this.supportService = supportService;
            this.playerUseCase = playerUseCase;
        }

        public async UniTask<LevelUpResult> LevelUpAsync(string supportCardUniqueId, int targetLevel)
        {
            if (string.IsNullOrEmpty(supportCardUniqueId))
            {
                throw new ArgumentException("supportCardUniqueId must not be null or empty", nameof(supportCardUniqueId));
            }

            var result = await supportService
                .WithHandledCodes(SupportErrors.InvalidTargetLevel, SupportErrors.NotEnoughPoints, PlayerErrors.NotEnoughGold, SupportErrors.NotOwned)
                .LevelUpAsync(new LevelUpRequest(supportCardUniqueId, targetLevel)).ToResult();
            if (result.IsError)
            {
                if (result.ErrorCode == SupportErrors.NotEnoughPoints)
                {
                    return LevelUpResult.Rejected(SupportResultKind.NotEnoughPoints);
                }

                if (result.ErrorCode == PlayerErrors.NotEnoughGold)
                {
                    return LevelUpResult.Rejected(SupportResultKind.NotEnoughGold);
                }

                if (result.ErrorCode == SupportErrors.InvalidTargetLevel)
                {
                    return LevelUpResult.Rejected(SupportResultKind.InvalidTarget);
                }

                if (result.ErrorCode == SupportErrors.NotOwned)
                {
                    return LevelUpResult.Rejected(SupportResultKind.NotOwned);
                }

                return LevelUpResult.Rejected(SupportResultKind.Failed);
            }

            // 詰め替えを先に済ませる。player_diff は基盤が返す前にリポジトリへ反映済み
            var levelUpResult = SupportMapper.ToLevelUpResult(result.Value);
            await RefreshAsync();
            return levelUpResult;
        }

        public async UniTask<LimitBreakResult> LimitBreakAsync(string supportCardUniqueId, string materialUniqueId)
        {
            if (string.IsNullOrEmpty(supportCardUniqueId))
            {
                throw new ArgumentException("supportCardUniqueId must not be null or empty", nameof(supportCardUniqueId));
            }

            if (string.IsNullOrEmpty(materialUniqueId))
            {
                throw new ArgumentException("materialUniqueId must not be null or empty", nameof(materialUniqueId));
            }

            if (supportCardUniqueId == materialUniqueId)
            {
                throw new ArgumentException("materialUniqueId must differ from supportCardUniqueId", nameof(materialUniqueId));
            }

            var result = await supportService
                .WithHandledCodes(SupportErrors.MaxLimitBreak, SupportErrors.InvalidMaterial, SupportErrors.NotOwned)
                .LimitBreakAsync(new LimitBreakRequest(supportCardUniqueId, materialUniqueId)).ToResult();
            if (result.IsError)
            {
                if (result.ErrorCode == SupportErrors.MaxLimitBreak)
                {
                    return LimitBreakResult.Rejected(SupportResultKind.MaxLimitBreak);
                }

                if (result.ErrorCode == SupportErrors.InvalidMaterial)
                {
                    return LimitBreakResult.Rejected(SupportResultKind.InvalidMaterial);
                }

                if (result.ErrorCode == SupportErrors.NotOwned)
                {
                    return LimitBreakResult.Rejected(SupportResultKind.NotOwned);
                }

                return LimitBreakResult.Rejected(SupportResultKind.Failed);
            }

            var limitBreakResult = SupportMapper.ToLimitBreakResult(result.Value);
            await RefreshAsync();
            return limitBreakResult;
        }

        private async UniTask RefreshAsync()
        {
            try
            {
                await playerUseCase.RefreshAsync();
            }
            catch (InvalidOperationException)
            {
                // 取り直しの失敗は成功を妨げない（通信の失敗のダイアログは ApiCore が出している）
            }
        }
    }
}

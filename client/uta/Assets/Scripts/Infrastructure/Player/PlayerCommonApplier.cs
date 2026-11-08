using System;
using Cysharp.Threading.Tasks;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Shared.Contracts.Common;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Player
{
    /// <summary>Common の player_diff をリポジトリ 3 つに反映する。同じ差分を 2 回当てても結果は変わらない。間で await しない。</summary>
    public sealed class PlayerCommonApplier : ICommonResponseApplier<AppCommon>
    {
        private readonly IPlayerRepository players;
        private readonly IIdolRepository idols;
        private readonly ISupportCardRepository supportCards;

        public PlayerCommonApplier(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards)
        {
            this.players = players;
            this.idols = idols;
            this.supportCards = supportCards;
        }

        public UniTask ApplyAsync(AppCommon common)
        {
            var diff = common?.PlayerDiff;
            if (diff == null)
            {
                return UniTask.CompletedTask;
            }

            var current = players.Find();
            if (current == null)
            {
                throw new InvalidOperationException("サインイン前に Common が届いた");
            }

            if (diff.HasBalance)
            {
                players.Upsert(new PlayerEntity(
                    current.UniqueId, diff.Level, diff.Exp, diff.Gold, diff.SupportPoints, diff.GachaCurrency,
                    current.HasActiveProduce, current.DiscardedProduce));
            }

            idols.Upsert(PlayerMapper.ToIdolEntities(diff.Idols));
            supportCards.Upsert(PlayerMapper.ToSupportCardEntities(diff.SupportCards));
            if (diff.RemovedSupportCardUniqueIds != null)
            {
                foreach (var uniqueId in diff.RemovedSupportCardUniqueIds)
                {
                    supportCards.Remove(uniqueId);
                }
            }

            return UniTask.CompletedTask;
        }
    }
}

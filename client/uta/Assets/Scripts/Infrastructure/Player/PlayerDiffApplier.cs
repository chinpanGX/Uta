using System;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Shared.Contracts.Common;

namespace Uta.Client.Infrastructure.Player
{
    /// <summary>player_diff をリポジトリ 3 つに反映する。同じ差分を 2 回当てても結果は変わらない。同期で、間で await しない（ADR 0021 の前提）。</summary>
    public sealed class PlayerDiffApplier
    {
        private readonly IPlayerRepository players;
        private readonly IIdolRepository idols;
        private readonly ISupportCardRepository supportCards;

        public PlayerDiffApplier(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards)
        {
            this.players = players;
            this.idols = idols;
            this.supportCards = supportCards;
        }

        /// <summary>diff は null でない（null の判定は呼ぶ側）。</summary>
        public void Apply(PlayerDiffDto diff)
        {
            var current = players.Find();
            if (current == null)
            {
                throw new InvalidOperationException("サインイン前に player_diff が届いた");
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
        }
    }
}

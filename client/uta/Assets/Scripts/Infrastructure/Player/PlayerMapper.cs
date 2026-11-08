using System.Collections.Generic;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Shared.Contracts.Player;

namespace Uta.Client.Infrastructure.Player
{
    /// <summary>DTO → Domain のエンティティ。Idols・SupportCards の配列が null なら空として扱う。</summary>
    public static class PlayerMapper
    {
        public static PlayerEntity ToPlayerEntity(SignInResponse response)
        {
            return new PlayerEntity(
                response.PlayerId, response.Level, response.Exp, response.Gold, response.SupportPoints, response.GachaCurrency,
                response.HasActiveProduce, response.DiscardedProduce);
        }

        public static IdolEntity ToIdolEntity(OwnedIdolDto dto)
        {
            return new IdolEntity(dto.IdolUid, dto.IdolId, dto.Training, dto.Awakening, dto.Shards);
        }

        public static SupportCardEntity ToSupportCardEntity(OwnedSupportCardDto dto)
        {
            return new SupportCardEntity(dto.SupportCardUid, dto.CardId, dto.Level, dto.LimitBreak);
        }

        public static List<IdolEntity> ToIdolEntities(OwnedIdolDto[] dtos)
        {
            var list = new List<IdolEntity>();
            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    list.Add(ToIdolEntity(dto));
                }
            }

            return list;
        }

        public static List<SupportCardEntity> ToSupportCardEntities(OwnedSupportCardDto[] dtos)
        {
            var list = new List<SupportCardEntity>();
            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    list.Add(ToSupportCardEntity(dto));
                }
            }

            return list;
        }
    }
}

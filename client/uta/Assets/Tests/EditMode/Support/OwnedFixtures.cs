using System.Collections.Generic;
using Uta.Client.Domain.Idol;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;

namespace Uta.Client.Tests
{
    /// <summary>所持一覧・サポートカードのテストのダミー値。マスタの型は Uta.MasterData. 付きで書く（Shared.Domain の enum と区別するため）。</summary>
    public static class OwnedFixtures
    {
        /// <summary>
        /// 共通の前提のマスタ:
        /// Idol: 1「アイドルA」SSR・Sense、2「アイドルB」R・Logic、3「アイドルC」SR・Sense。
        /// SupportCard: 10「サポートA」SSR・Singing・Free、11「サポートB」SR・Rhythm・Sense、12「サポートC」R・Expression・Logic。
        /// SupportAbility: (10, Order 1, InitialParam, Singing, ValueAtLv1 10, ValueAtMax 50)。
        /// SupportLevelCap: R・SR・SSR それぞれ LimitBreak 0〜4 で MaxLevel = 40 + LimitBreak * 5。
        /// SupportLevelCost: R・SR・SSR それぞれ Level 1〜64 で SupportPoints 100・Gold 50。
        /// </summary>
        public static Uta.MasterData.MemoryDatabase CreateMaster()
        {
            var rarities = new[] { Uta.MasterData.Rarity.R, Uta.MasterData.Rarity.SR, Uta.MasterData.Rarity.SSR };
            var caps = new List<Uta.MasterData.SupportLevelCap>();
            var costs = new List<Uta.MasterData.SupportLevelCost>();
            foreach (var rarity in rarities)
            {
                for (var limitBreak = 0; limitBreak <= 4; limitBreak++)
                {
                    caps.Add(new Uta.MasterData.SupportLevelCap { Rarity = rarity, LimitBreak = limitBreak, MaxLevel = 40 + limitBreak * 5 });
                }

                for (var level = 1; level <= 64; level++)
                {
                    costs.Add(new Uta.MasterData.SupportLevelCost { Rarity = rarity, Level = level, SupportPoints = 100, Gold = 50 });
                }
            }

            return TestMasterData.Build(b =>
            {
                b.Append(new[]
                {
                    Idol(1, "アイドルA", Uta.MasterData.Rarity.SSR, Uta.MasterData.Plan.Sense),
                    Idol(2, "アイドルB", Uta.MasterData.Rarity.R, Uta.MasterData.Plan.Logic),
                    Idol(3, "アイドルC", Uta.MasterData.Rarity.SR, Uta.MasterData.Plan.Sense),
                });
                b.Append(new[]
                {
                    SupportCard(10, "サポートA", Uta.MasterData.Rarity.SSR, Uta.MasterData.SupportType.Singing, Uta.MasterData.Plan.Free),
                    SupportCard(11, "サポートB", Uta.MasterData.Rarity.SR, Uta.MasterData.SupportType.Rhythm, Uta.MasterData.Plan.Sense),
                    SupportCard(12, "サポートC", Uta.MasterData.Rarity.R, Uta.MasterData.SupportType.Expression, Uta.MasterData.Plan.Logic),
                });
                b.Append(new[]
                {
                    new Uta.MasterData.SupportAbility
                    {
                        SupportCardId = 10,
                        Order = 1,
                        Kind = Uta.MasterData.SupportAbilityKind.InitialParam,
                        Param = Uta.MasterData.ParamKind.Singing,
                        ValueAtLv1 = 10,
                        ValueAtMax = 50,
                    },
                });
                b.Append(caps);
                b.Append(costs);
            });
        }

        /// <summary>playerRepository に PlayerEntity("P1", 1, 0, gold, supportPoints, 0, false, false) を Upsert。</summary>
        public static void SetPlayer(PlayerRepository playerRepository, int gold = 1000, int supportPoints = 1000)
        {
            playerRepository.Upsert(new PlayerEntity("P1", 1, 0, gold, supportPoints, 0, false, false));
        }

        /// <summary>
        /// idolRepository に IdolEntity("I2", 2, 0, 0, 0)・("I1", 1, 0, 0, 3)、supportCardRepository に ("01A", 10, 3, 0)・("01B", 11, 5, 1)・("01C", 12, 1, 0)・("01D", 10, 1, 0)
        /// をこの順に Upsert（Clear してから）。
        /// </summary>
        public static void SetOwned(IdolRepository idolRepository, SupportCardRepository supportCardRepository)
        {
            idolRepository.Clear();
            idolRepository.Upsert(new IdolEntity("I2", 2, 0, 0, 0));
            idolRepository.Upsert(new IdolEntity("I1", 1, 0, 0, 3));
            supportCardRepository.Clear();
            supportCardRepository.Upsert(new SupportCardEntity("01A", 10, 3, 0));
            supportCardRepository.Upsert(new SupportCardEntity("01B", 11, 5, 1));
            supportCardRepository.Upsert(new SupportCardEntity("01C", 12, 1, 0));
            supportCardRepository.Upsert(new SupportCardEntity("01D", 10, 1, 0));
        }

        private static Uta.MasterData.Idol Idol(int idolId, string name, Uta.MasterData.Rarity rarity, Uta.MasterData.Plan plan)
        {
            return new Uta.MasterData.Idol
            {
                IdolId = idolId,
                Name = name,
                Rarity = rarity,
                Plan = plan,
                MaxHp = 30,
                FirstParam = Uta.MasterData.ParamKind.Singing,
                SecondParam = Uta.MasterData.ParamKind.Rhythm,
                ThirdParam = Uta.MasterData.ParamKind.Expression,
            };
        }

        private static Uta.MasterData.SupportCard SupportCard(int supportCardId, string name, Uta.MasterData.Rarity rarity,
            Uta.MasterData.SupportType type, Uta.MasterData.Plan plan)
        {
            return new Uta.MasterData.SupportCard
            {
                SupportCardId = supportCardId,
                Name = name,
                Rarity = rarity,
                Type = type,
                Plan = plan,
            };
        }
    }
}

using System;
using NUnit.Framework;
using Uta.Client.Application.Produce;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Client.Presentation.Formation;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class FormationDtoFactoryTests
    {
        private static readonly string[] EmptySlots = new string[5];

        private static FormationDtoFactory Build(int scenarioCount)
        {
            var db = TestMasterData.Build(b =>
            {
                b.Append(new[]
                {
                    TestMasterData.Idol(1, "アイドル1", Rarity.SR),
                    new Idol { IdolId = 2, Name = "アイドル2", Rarity = Rarity.SSR, Plan = Plan.Logic, MaxHp = 30 },
                });
                b.Append(new[] { new SupportCard { SupportCardId = 20, Name = "カード20", Rarity = Rarity.SR, Plan = Plan.Logic } });
                var scenarios = new Scenario[scenarioCount];
                for (var i = 0; i < scenarioCount; i++)
                {
                    scenarios[i] = new Scenario { ScenarioId = i + 1, Name = "シナリオ" + (i + 1) };
                }

                b.Append(scenarios);
            });
            var supportCards = new SupportCardRepository();
            supportCards.Upsert(new SupportCardEntity("U20", 20, 3, 0));
            return new FormationDtoFactory(supportCards, new ScenarioResolver(db), db);
        }

        [Test]
        public void Create_IdolAndEmptySlotsAndScenario()
        {
            var dto = Build(2).Create(2, EmptySlots, 0);

            Assert.AreEqual("育成アイドル: アイドル2 [SSR] 理論", dto.IdolLabel);
            Assert.AreEqual(5, dto.Slots.Count);
            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual("枠" + (i + 1) + ": （空）", dto.Slots[i].Label);
                Assert.IsFalse(dto.Slots[i].Filled);
            }

            Assert.AreEqual("シナリオ: シナリオ1", dto.ScenarioLabel);
            Assert.IsTrue(dto.CanChangeScenario);
            Assert.IsTrue(dto.CanStart);
        }

        [Test]
        public void Create_NoIdol_StartDisabled()
        {
            var dto = Build(2).Create(0, EmptySlots, 0);

            Assert.AreEqual("育成アイドル: 未選択", dto.IdolLabel);
            Assert.IsFalse(dto.CanStart);
        }

        [Test]
        public void Create_FilledSlot_ShowsCard()
        {
            var dto = Build(1).Create(2, new[] { null, "U20", null, null, null }, 0);

            Assert.AreEqual("枠2: カード20 [SR] Lv 3", dto.Slots[1].Label);
            Assert.IsTrue(dto.Slots[1].Filled);
            Assert.IsFalse(dto.Slots[0].Filled);
        }

        [Test]
        public void Create_UnknownUid_ShowsEmpty()
        {
            var dto = Build(1).Create(2, new[] { "ZZ", null, null, null, null }, 0);

            Assert.AreEqual("枠1: （空）", dto.Slots[0].Label);
            Assert.IsFalse(dto.Slots[0].Filled);
        }

        [Test]
        public void Create_NoScenarios_ShowsNoneAndDisablesStart()
        {
            var dto = Build(0).Create(2, EmptySlots, 0);

            Assert.AreEqual("シナリオ: なし", dto.ScenarioLabel);
            Assert.IsFalse(dto.CanChangeScenario);
            Assert.IsFalse(dto.CanStart);
        }

        [Test]
        public void Create_SingleScenario_CannotChange()
        {
            var dto = Build(1).Create(2, EmptySlots, 0);

            Assert.IsFalse(dto.CanChangeScenario);
            Assert.IsTrue(dto.CanStart);
        }

        [Test]
        public void Create_ScenarioIndex_PicksName()
        {
            var dto = Build(2).Create(2, EmptySlots, 1);

            Assert.AreEqual("シナリオ: シナリオ2", dto.ScenarioLabel);
        }

        [Test]
        public void Create_NullSlots_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Build(1).Create(2, null, 0));
        }
    }
}

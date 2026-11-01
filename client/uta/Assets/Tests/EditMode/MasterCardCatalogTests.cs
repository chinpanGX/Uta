using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using DomainLesson = Uta.Shared.Domain.Lesson;

namespace Uta.Client.Infrastructure.Tests
{
    public class MasterCardCatalogTests
    {
        public static readonly object[][] EnumPairs =
        {
            new object[] { typeof(Uta.MasterData.Plan), typeof(DomainLesson.Plan) },
            new object[] { typeof(Uta.MasterData.CardType), typeof(DomainLesson.CardType) },
            new object[] { typeof(Uta.MasterData.Rarity), typeof(DomainLesson.Rarity) },
            new object[] { typeof(Uta.MasterData.ParamKind), typeof(DomainLesson.ParamKind) },
            new object[] { typeof(Uta.MasterData.EffectOp), typeof(DomainLesson.EffectOp) },
            new object[] { typeof(Uta.MasterData.PersistentTrigger), typeof(DomainLesson.PersistentTrigger) },
            new object[] { typeof(Uta.MasterData.StatusKind), typeof(DomainLesson.StatusKind) },
            new object[] { typeof(Uta.MasterData.CostKind), typeof(DomainLesson.CostKind) },
            new object[] { typeof(Uta.MasterData.ConditionSubject), typeof(DomainLesson.ConditionSubject) },
            new object[] { typeof(Uta.MasterData.Comparator), typeof(DomainLesson.Comparator) },
            new object[] { typeof(Uta.MasterData.CardFaceKind), typeof(DomainLesson.CardFaceKind) },
        };

        private static byte[] ReadBundled()
        {
            return File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath);
        }

        private static DomainLesson.CardCatalog CreateCatalog()
        {
            return MasterCardCatalog.Create(MasterDataLoader.Load(ReadBundled()));
        }

        [Test]
        public void Create_Mvp_Has36Cards()
        {
            var catalog = CreateCatalog();

            Assert.AreEqual(36, catalog.CardIds.Count);
            Assert.AreEqual(1, catalog.CardIds[0]);
            Assert.AreEqual(215, catalog.CardIds[catalog.CardIds.Count - 1]);
            Assert.AreEqual(catalog.CardIds.OrderBy(x => x).ToList(), catalog.CardIds.ToList());
        }

        [Test]
        public void Get_F01_HasNormalAndEnhancedFaces()
        {
            var card = CreateCatalog().Get(1);

            Assert.AreEqual(DomainLesson.Plan.Free, card.Plan);
            Assert.AreEqual(DomainLesson.CardType.Active, card.Type);
            Assert.AreEqual(DomainLesson.Rarity.N, card.Rarity);
            Assert.AreEqual(1, card.Base.Costs.Count);
            Assert.AreEqual(DomainLesson.CostKind.Consume, card.Base.Costs[0].Kind);
            Assert.AreEqual(4, card.Base.Costs[0].Value);
            Assert.AreEqual(1, card.Base.Effects.Count);
            Assert.AreEqual(DomainLesson.EffectOp.GainParam, card.Base.Effects[0].Op);
            Assert.AreEqual(9, card.Base.Effects[0].Value);
            Assert.NotNull(card.Enhanced);
            Assert.AreEqual(DomainLesson.CostKind.Consume, card.Enhanced.Costs[0].Kind);
            Assert.AreEqual(3, card.Enhanced.Costs[0].Value);
            Assert.AreEqual(14, card.Enhanced.Effects[0].Value);
        }

        [Test]
        public void Get_S14_HasConditionedEffect()
        {
            var effects = CreateCatalog().Get(114).Base.Effects;

            Assert.AreEqual(2, effects.Count);
            Assert.AreEqual(1, effects[1].Conditions.Count);
            Assert.AreEqual(DomainLesson.ConditionSubject.Focus, effects[1].Conditions[0].Subject);
            Assert.AreEqual(DomainLesson.Comparator.GreaterOrEqual, effects[1].Conditions[0].Comparator);
            Assert.AreEqual(3, effects[1].Conditions[0].Value);
        }

        [Test]
        public void Get_S13_HasPersistentWithChild()
        {
            var persistent = CreateCatalog().Get(113).Base.Effects.Single(x => x.Op == DomainLesson.EffectOp.AddPersistent);

            Assert.AreEqual(DomainLesson.PersistentTrigger.OnActiveCardUse, persistent.Trigger);
            Assert.AreEqual(1, persistent.Children.Count);
            Assert.AreEqual(DomainLesson.EffectOp.AddStatus, persistent.Children[0].Op);
            Assert.AreEqual(DomainLesson.StatusKind.Focus, persistent.Children[0].Status);
            Assert.AreEqual(1, persistent.Children[0].Value);
        }

        [Test]
        public void Get_S04_HasUseCondition()
        {
            var conditions = CreateCatalog().Get(104).Base.UseConditions;

            Assert.AreEqual(1, conditions.Count);
            Assert.AreEqual(DomainLesson.ConditionSubject.GoodConditionTurns, conditions[0].Subject);
            Assert.AreEqual(DomainLesson.Comparator.GreaterOrEqual, conditions[0].Comparator);
            Assert.AreEqual(2, conditions[0].Value);
        }

        [Test]
        public void Create_Null_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => MasterCardCatalog.Create(null));
        }

        [TestCaseSource(nameof(EnumPairs))]
        public void EnumMirrors_MatchDomain(Type masterEnum, Type domainEnum)
        {
            Assert.AreEqual(Enum.GetNames(masterEnum), Enum.GetNames(domainEnum), masterEnum.Name);
            foreach (var name in Enum.GetNames(masterEnum))
            {
                Assert.AreEqual(
                    Convert.ToInt32(Enum.Parse(masterEnum, name)),
                    Convert.ToInt32(Enum.Parse(domainEnum, name)),
                    masterEnum.Name + "." + name);
            }
        }

        [UnityTest]
        public IEnumerator LoadFromStreamingAssetsAsync_ReturnsCatalog()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var catalog = await MasterCardCatalog.LoadFromStreamingAssetsAsync(CancellationToken.None);

                Assert.AreEqual(36, catalog.CardIds.Count);
            });
        }
    }
}

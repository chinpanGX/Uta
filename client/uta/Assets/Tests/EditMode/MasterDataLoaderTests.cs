using System;
using System.Collections;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.MasterData;

namespace Uta.Client.Infrastructure.Tests
{
    public class MasterDataLoaderTests
    {
        private static byte[] ReadBundled()
        {
            return File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath);
        }

        [Test]
        public void Load_Bundled_ReturnsDatabase()
        {
            var db = MasterDataLoader.Load(ReadBundled());

            Assert.AreEqual(36, db.CardTable.Count, "CardTable");
            Assert.AreEqual(72, db.CardFaceTable.Count, "CardFaceTable");
            Assert.AreEqual(72, db.CardCostTable.Count, "CardCostTable");
            Assert.AreEqual(129, db.EffectTable.Count, "EffectTable");
            Assert.AreEqual(14, db.ConditionTable.Count, "ConditionTable");
            Assert.AreEqual(2, db.ScenarioTable.Count, "ScenarioTable");
            Assert.AreEqual(16, db.ScenarioWeekTable.Count, "ScenarioWeekTable");
            Assert.AreEqual(17, db.WeekActionSlotTable.Count, "WeekActionSlotTable");
            Assert.AreEqual(12, db.ClassOptionTable.Count, "ClassOptionTable");
            Assert.AreEqual(4, db.ExamPatternTable.Count, "ExamPatternTable");
            Assert.AreEqual(6, db.ExamRankRewardTable.Count, "ExamRankRewardTable");
            Assert.AreEqual(1, db.ScoreBonusSettingTable.Count, "ScoreBonusSettingTable");
            Assert.AreEqual(7, db.EvaluationScoreBandTable.Count, "EvaluationScoreBandTable");
            Assert.AreEqual(13, db.EvaluationRankTable.Count, "EvaluationRankTable");
            Assert.AreEqual(4, db.OfferRarityWeightTable.Count, "OfferRarityWeightTable");
            Assert.AreEqual(99, db.PlayerLevelExpTable.Count, "PlayerLevelExpTable");
            Assert.AreEqual(4, db.IdolTable.Count, "IdolTable");
            Assert.AreEqual(12, db.IdolRecommendDeckTable.Count, "IdolRecommendDeckTable");
            Assert.AreEqual(5, db.CommonDeckCardTable.Count, "CommonDeckCardTable");
            Assert.AreEqual(10, db.SupportCardTable.Count, "SupportCardTable");
            Assert.AreEqual(16, db.SupportAbilityTable.Count, "SupportAbilityTable");
            Assert.AreEqual(147, db.SupportLevelCostTable.Count, "SupportLevelCostTable");
            Assert.AreEqual(15, db.SupportLevelCapTable.Count, "SupportLevelCapTable");
            Assert.AreEqual(5, db.InitialSupportCardTable.Count, "InitialSupportCardTable");
            Assert.AreEqual(1, db.InitialSettingTable.Count, "InitialSettingTable");
            Assert.AreEqual(1, db.GachaTable.Count, "GachaTable");
            Assert.AreEqual(2, db.GachaDetailTableTable.Count, "GachaDetailTableTable");
            Assert.AreEqual(21, db.GachaTableEntryTable.Count, "GachaTableEntryTable");
            Assert.AreEqual(2, db.GachaCeilingItemTable.Count, "GachaCeilingItemTable");
            Assert.AreEqual(6, db.DuplicateConversionTable.Count, "DuplicateConversionTable");
        }

        [Test]
        public void Load_NotMasterData_ThrowsInvalidOperation()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => MasterDataLoader.Load(new byte[] { 1, 2, 3 }));
            StringAssert.Contains("NotMasterData", exception.Message);
            StringAssert.DoesNotContain(MasterDataSchema.Hash, exception.Message);
        }

        [Test]
        public void Load_Null_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => MasterDataLoader.Load(null));
        }

        [Test]
        public void Load_TamperedPayload_ThrowsInvalidOperation()
        {
            var bytes = ReadBundled();
            bytes[bytes.Length - 1] ^= 0xFF;
            Assert.Throws<InvalidOperationException>(() => MasterDataLoader.Load(bytes));
        }

        [UnityTest]
        public IEnumerator LoadFromStreamingAssetsAsync_ReturnsDatabase()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var db = await MasterDataLoader.LoadFromStreamingAssetsAsync();
                Assert.AreEqual(36, db.CardTable.Count);
            });
        }

        [UnityTest]
        public IEnumerator LoadFromStreamingAssetsAsync_Cancelled_Throws()
        {
            return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    await MasterDataLoader.LoadFromStreamingAssetsAsync(new CancellationToken(true));
                    Assert.Fail("OperationCanceledException が投げられなかった");
                }
                catch (OperationCanceledException)
                {
                }
            });
        }
    }
}

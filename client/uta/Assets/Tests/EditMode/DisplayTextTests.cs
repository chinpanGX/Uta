using NUnit.Framework;
using Uta.Client.Presentation.Common;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Tests
{
    public sealed class DisplayTextTests
    {
        [Test]
        public void RarityName_AllValues()
        {
            Assert.AreEqual("-", DisplayText.RarityName(Rarity.Nothing));
            Assert.AreEqual("N", DisplayText.RarityName(Rarity.N));
            Assert.AreEqual("R", DisplayText.RarityName(Rarity.R));
            Assert.AreEqual("SR", DisplayText.RarityName(Rarity.SR));
            Assert.AreEqual("SSR", DisplayText.RarityName(Rarity.SSR));
            Assert.AreEqual("-", DisplayText.RarityName((Rarity)99));
        }

        [Test]
        public void PlanName_AllValues()
        {
            Assert.AreEqual("-", DisplayText.PlanName(Plan.Nothing));
            Assert.AreEqual("共通", DisplayText.PlanName(Plan.Free));
            Assert.AreEqual("感性", DisplayText.PlanName(Plan.Sense));
            Assert.AreEqual("理論", DisplayText.PlanName(Plan.Logic));
            Assert.AreEqual("-", DisplayText.PlanName((Plan)99));
        }

        [Test]
        public void SupportTypeName_AllValues()
        {
            Assert.AreEqual("-", DisplayText.SupportTypeName(SupportType.Nothing));
            Assert.AreEqual("歌唱", DisplayText.SupportTypeName(SupportType.Singing));
            Assert.AreEqual("律動", DisplayText.SupportTypeName(SupportType.Rhythm));
            Assert.AreEqual("表現", DisplayText.SupportTypeName(SupportType.Expression));
            Assert.AreEqual("アシスト", DisplayText.SupportTypeName(SupportType.Assist));
            Assert.AreEqual("-", DisplayText.SupportTypeName((SupportType)99));
        }

        [Test]
        public void ParamName_AllValues()
        {
            Assert.AreEqual("-", DisplayText.ParamName(ParamKind.Nothing));
            Assert.AreEqual("歌唱", DisplayText.ParamName(ParamKind.Singing));
            Assert.AreEqual("律動", DisplayText.ParamName(ParamKind.Rhythm));
            Assert.AreEqual("表現", DisplayText.ParamName(ParamKind.Expression));
            Assert.AreEqual("-", DisplayText.ParamName((ParamKind)99));
        }

        [Test]
        public void Ability_InitialParam()
        {
            Assert.AreEqual("初期歌唱 +49", DisplayText.Ability(SupportAbilityKind.InitialParam, ParamKind.Singing, 49));
        }

        [Test]
        public void Ability_ParamBonus_PerThousand()
        {
            Assert.AreEqual("歌唱ボーナス +8.5%", DisplayText.Ability(SupportAbilityKind.ParamBonus, ParamKind.Singing, 85));
            Assert.AreEqual("歌唱ボーナス +10.0%", DisplayText.Ability(SupportAbilityKind.ParamBonus, ParamKind.Singing, 100));
        }

        [Test]
        public void Ability_MaxHp()
        {
            Assert.AreEqual("最大体力 +9", DisplayText.Ability(SupportAbilityKind.MaxHp, ParamKind.Nothing, 9));
        }
    }
}

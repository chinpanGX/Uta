using System;
using NUnit.Framework;
using Uta.Client.Presentation.Gacha;

namespace Uta.Client.Tests
{
    public sealed class GachaTextsTests
    {
        [Test]
        public void Percent_Formats()
        {
            Assert.AreEqual("0.00%", GachaTexts.Percent(0));
            Assert.AreEqual("0.05%", GachaTexts.Percent(5));
            Assert.AreEqual("12.34%", GachaTexts.Percent(1234));
            Assert.AreEqual("100.00%", GachaTexts.Percent(10000));
            Assert.Throws<ArgumentOutOfRangeException>(() => GachaTexts.Percent(-1));
        }

        [Test]
        public void Kind_Texts()
        {
            Assert.AreEqual("育成アイドル", GachaTexts.Kind(Uta.MasterData.ItemKind.Idol));
            Assert.AreEqual("サポートカード", GachaTexts.Kind(Uta.MasterData.ItemKind.SupportCard));
            Assert.Throws<ArgumentOutOfRangeException>(() => GachaTexts.Kind((Uta.MasterData.ItemKind)99));
        }
    }
}

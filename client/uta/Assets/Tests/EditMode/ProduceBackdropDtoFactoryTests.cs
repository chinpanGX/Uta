using System;
using NUnit.Framework;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Tests
{
    public sealed class ProduceBackdropDtoFactoryTests
    {
        [Test]
        public void Create_FormatsHeader()
        {
            var p = TestProduce.StateResult(week: 3, singing: 100, rhythm: 110, expression: 120, hp: 25, maxHp: 30);

            var dto = new ProduceBackdropDtoFactory().Create(p);

            Assert.AreEqual("週 3\n歌唱 100　律動 110　表現 120　体力 25 / 30", dto.Header);
        }

        [Test]
        public void Create_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProduceBackdropDtoFactory().Create(null));
        }
    }
}

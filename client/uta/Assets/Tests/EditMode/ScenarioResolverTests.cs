using System;
using NUnit.Framework;
using Uta.Client.Application.Produce;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    public sealed class ScenarioResolverTests
    {
        private static ScenarioResolver Build(params int[] ids)
        {
            var db = TestMasterData.Build(b =>
            {
                var rows = new Scenario[ids.Length];
                for (var i = 0; i < ids.Length; i++)
                {
                    rows[i] = new Scenario { ScenarioId = ids[i], Name = "S" + ids[i] };
                }

                b.Append(rows);
            });
            return new ScenarioResolver(db);
        }

        [Test]
        public void Count_ReturnsNumberOfScenarios()
        {
            Assert.AreEqual(2, Build(2, 1).Count);
            Assert.AreEqual(0, Build().Count);
        }

        [Test]
        public void IdAt_And_NameAt_AreSortedByScenarioId()
        {
            var resolver = Build(3, 1, 2);

            Assert.AreEqual(1, resolver.IdAt(0));
            Assert.AreEqual(3, resolver.IdAt(2));
            Assert.AreEqual("S2", resolver.NameAt(1));
        }

        [Test]
        public void IdAt_OutOfRange_Throws()
        {
            var resolver = Build(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => resolver.IdAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => resolver.IdAt(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => resolver.NameAt(1));
        }
    }
}

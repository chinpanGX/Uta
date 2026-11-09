using System;
using NUnit.Framework;
using Uta.Client.Application.Owned;

namespace Uta.Client.Tests
{
    public sealed class OwnedSelectionConditionTests
    {
        [Test]
        public void ForIdol_NullExcluded_IsEmpty()
        {
            var condition = OwnedSelectionCondition.ForIdol();

            Assert.AreEqual(OwnedSelectionKind.Idol, condition.Kind);
            Assert.AreEqual(0, condition.ExcludedIdolIds.Count);
            Assert.AreEqual(0, condition.ExcludedUids.Count);
            Assert.AreEqual(0, condition.IdolId);
        }

        [Test]
        public void ForSupportCard_NullExcluded_IsEmpty()
        {
            var condition = OwnedSelectionCondition.ForSupportCard(3, null);

            Assert.AreEqual(OwnedSelectionKind.SupportCard, condition.Kind);
            Assert.AreEqual(3, condition.IdolId);
            Assert.AreEqual(0, condition.ExcludedUids.Count);
            Assert.AreEqual(0, condition.ExcludedIdolIds.Count);
        }

        [Test]
        public void ForLimitBreakMaterial_SetsFields()
        {
            var condition = OwnedSelectionCondition.ForLimitBreakMaterial(10, "01A");

            Assert.AreEqual(OwnedSelectionKind.LimitBreakMaterial, condition.Kind);
            Assert.AreEqual(10, condition.MaterialCardId);
            Assert.AreEqual("01A", condition.MaterialTargetUid);
        }

        [Test]
        public void ForLimitBreakMaterial_EmptyTarget_Throws()
        {
            Assert.Throws<ArgumentException>(() => OwnedSelectionCondition.ForLimitBreakMaterial(10, ""));
            Assert.Throws<ArgumentException>(() => OwnedSelectionCondition.ForLimitBreakMaterial(10, null));
        }
    }
}

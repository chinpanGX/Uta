using NUnit.Framework;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Tests
{
    public sealed class PageStackTests
    {
        [Test]
        public void PopToRoot_ThreePages_PopsTwo()
        {
            var log = new CallLog();

            new PageStack(() => 3, () => 0).PopToRootAsync(new FakeScreenNavigator(log));

            CollectionAssert.AreEqual(new[] { "PopPage:2" }, log.Entries);
        }

        [Test]
        public void PopToRoot_WithModals_PopsModalsFirst()
        {
            var log = new CallLog();

            new PageStack(() => 2, () => 1).PopToRootAsync(new FakeScreenNavigator(log));

            CollectionAssert.AreEqual(new[] { "PopModal:1", "PopPage:1" }, log.Entries);
        }

        [Test]
        public void PopToRoot_OnePage_DoesNothing()
        {
            var log = new CallLog();

            new PageStack(() => 1, () => 0).PopToRootAsync(new FakeScreenNavigator(log));

            Assert.IsEmpty(log.Entries);
        }

        [Test]
        public void PopToRoot_Empty_DoesNothing()
        {
            var log = new CallLog();

            new PageStack(() => 0, () => 0).PopToRootAsync(new FakeScreenNavigator(log));

            Assert.IsEmpty(log.Entries);
        }
    }
}

using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Domain.Player;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Presentation.Home;

namespace Uta.Client.Tests
{
    public sealed class HomeEntryTests
    {
        [UnityTest]
        public IEnumerator StartAsync_NoPlayer_GoesTitle() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var entry = new HomeEntry(new FakeScreenNavigator(log), new PlayerRepository(), new FakeSceneNavigator(log));

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { "GoToTitle" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator StartAsync_WithPlayer_PushesHome() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var players = new PlayerRepository();
            players.Upsert(new PlayerEntity("P1", 1, 0, 0, 0, 0, false, false));
            var entry = new HomeEntry(new FakeScreenNavigator(log), players, new FakeSceneNavigator(log));

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { "PushPage:HomePresenter" }, log.Entries);
        });
    }
}

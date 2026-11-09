using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Presentation.Startup;
using Uta.Client.Presentation.Title;

namespace Uta.Client.Tests
{
    public sealed class TitleEntryTests
    {
        [UnityTest]
        public IEnumerator StartAsync_Normal_PushesTitle() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var entry = new TitleEntry(new FakeScreenNavigator(log), new StartupOptions(false), null);

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { "PushPage:TitlePresenter" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator StartAsync_QuitAfterCheck_Success_QuitsWithZero() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var codes = new List<int>();
            var deviceAuth = new FakeDeviceAuthUseCase();
            var player = new FakePlayerUseCase();
            var diagnostics = new TitleDiagnostics(deviceAuth, player, new ConnectivityCheck(deviceAuth, player), code => codes.Add(code));
            var entry = new TitleEntry(new FakeScreenNavigator(log), new StartupOptions(true), diagnostics);

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { 0 }, codes);
            Assert.IsFalse(log.Entries.Exists(e => e.StartsWith("PushPage:")));
        });

        [UnityTest]
        public IEnumerator StartAsync_QuitAfterCheck_SignInFails_QuitsWithOne() => UniTask.ToCoroutine(async () =>
        {
            var log = new CallLog();
            var codes = new List<int>();
            var deviceAuth = new FakeDeviceAuthUseCase();
            deviceAuth.SignInResults.Enqueue(() => throw new InvalidOperationException("x"));
            var player = new FakePlayerUseCase();
            var diagnostics = new TitleDiagnostics(deviceAuth, player, new ConnectivityCheck(deviceAuth, player), code => codes.Add(code));
            var entry = new TitleEntry(new FakeScreenNavigator(log), new StartupOptions(true), diagnostics);

            await entry.StartAsync();

            CollectionAssert.AreEqual(new[] { 1 }, codes);
            Assert.IsFalse(log.Entries.Exists(e => e.StartsWith("PushPage:")));
        });
    }
}

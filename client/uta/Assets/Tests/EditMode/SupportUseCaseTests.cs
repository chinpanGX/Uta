using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Support;
using Uta.Client.Infrastructure.Support;
using Uta.Shared.Contracts.Player;
using Uta.Shared.Contracts.Support;

namespace Uta.Client.Tests
{
    /// <summary>手順のテスト。偽の ISupportService・偽の IPlayerUseCase。</summary>
    public sealed class SupportUseCaseTests
    {
        private FakeSupportService service;
        private FakePlayerUseCase player;
        private SupportUseCase useCase;

        [SetUp]
        public void SetUp()
        {
            service = new FakeSupportService();
            player = new FakePlayerUseCase();
            useCase = new SupportUseCase(service, player);
        }

        [UnityTest]
        public IEnumerator LevelUp_Success_RefreshesAndReturnsResult() => UniTask.ToCoroutine(async () =>
        {
            service.LevelUpResults.Enqueue(FakeSupportService.Ok(new LevelUpResponse(4, 900, 950)));

            var result = await useCase.LevelUpAsync("01A", 4);

            Assert.AreEqual(SupportResultKind.Succeeded, result.Kind);
            Assert.AreEqual(4, result.Level);
            Assert.AreEqual(1, player.RefreshCount);
            Assert.AreEqual(new[] { "LevelUp:01A,4" }, service.Calls.ToArray());
        });

        [UnityTest]
        public IEnumerator LevelUp_RefreshFails_StillSucceeds() => UniTask.ToCoroutine(async () =>
        {
            service.LevelUpResults.Enqueue(FakeSupportService.Ok(new LevelUpResponse(4, 900, 950)));
            player.RefreshResults.Enqueue(() => throw new InvalidOperationException("sign-in failed: x"));

            var result = await useCase.LevelUpAsync("01A", 4);

            Assert.AreEqual(SupportResultKind.Succeeded, result.Kind);
        });

        [UnityTest]
        public IEnumerator LevelUp_ErrorCodes_MapToKinds() => UniTask.ToCoroutine(async () =>
        {
            service.LevelUpResults.Enqueue(FakeSupportService.Fail<LevelUpResponse>(SupportErrors.NotEnoughPoints));
            service.LevelUpResults.Enqueue(FakeSupportService.Fail<LevelUpResponse>(PlayerErrors.NotEnoughGold));
            service.LevelUpResults.Enqueue(FakeSupportService.Fail<LevelUpResponse>(SupportErrors.InvalidTargetLevel));
            service.LevelUpResults.Enqueue(FakeSupportService.Fail<LevelUpResponse>(SupportErrors.NotOwned));

            Assert.AreEqual(SupportResultKind.NotEnoughPoints, (await useCase.LevelUpAsync("01A", 4)).Kind);
            Assert.AreEqual(SupportResultKind.NotEnoughGold, (await useCase.LevelUpAsync("01A", 4)).Kind);
            Assert.AreEqual(SupportResultKind.InvalidTarget, (await useCase.LevelUpAsync("01A", 4)).Kind);
            Assert.AreEqual(SupportResultKind.NotOwned, (await useCase.LevelUpAsync("01A", 4)).Kind);
            Assert.AreEqual(0, player.RefreshCount);
        });

        [UnityTest]
        public IEnumerator LevelUp_OtherError_ReturnsFailed() => UniTask.ToCoroutine(async () =>
        {
            service.LevelUpResults.Enqueue(FakeSupportService.Fail<LevelUpResponse>(SupportErrors.MaxLimitBreak));

            var result = await useCase.LevelUpAsync("01A", 4);

            Assert.AreEqual(SupportResultKind.Failed, result.Kind);
        });

        [UnityTest]
        public IEnumerator LevelUp_EmptyUid_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await useCase.LevelUpAsync("", 4);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
            }

            Assert.IsEmpty(service.Calls);
        });

        [UnityTest]
        public IEnumerator LimitBreak_Success_RefreshesAndReturnsResult() => UniTask.ToCoroutine(async () =>
        {
            service.LimitBreakResults.Enqueue(FakeSupportService.Ok(new LimitBreakResponse(1, 45)));

            var result = await useCase.LimitBreakAsync("01A", "01D");

            Assert.AreEqual(SupportResultKind.Succeeded, result.Kind);
            Assert.AreEqual(1, result.LimitBreak);
            Assert.AreEqual(45, result.MaxLevel);
            Assert.AreEqual(1, player.RefreshCount);
            Assert.AreEqual(new[] { "LimitBreak:01A,01D" }, service.Calls.ToArray());
        });

        [UnityTest]
        public IEnumerator LimitBreak_ErrorCodes_MapToKinds() => UniTask.ToCoroutine(async () =>
        {
            service.LimitBreakResults.Enqueue(FakeSupportService.Fail<LimitBreakResponse>(SupportErrors.MaxLimitBreak));
            service.LimitBreakResults.Enqueue(FakeSupportService.Fail<LimitBreakResponse>(SupportErrors.InvalidMaterial));
            service.LimitBreakResults.Enqueue(FakeSupportService.Fail<LimitBreakResponse>(SupportErrors.NotOwned));
            service.LimitBreakResults.Enqueue(FakeSupportService.Fail<LimitBreakResponse>(SupportErrors.NotEnoughPoints));

            Assert.AreEqual(SupportResultKind.MaxLimitBreak, (await useCase.LimitBreakAsync("01A", "01D")).Kind);
            Assert.AreEqual(SupportResultKind.InvalidMaterial, (await useCase.LimitBreakAsync("01A", "01D")).Kind);
            Assert.AreEqual(SupportResultKind.NotOwned, (await useCase.LimitBreakAsync("01A", "01D")).Kind);
            Assert.AreEqual(SupportResultKind.Failed, (await useCase.LimitBreakAsync("01A", "01D")).Kind);
            Assert.AreEqual(0, player.RefreshCount);
        });

        [UnityTest]
        public IEnumerator LimitBreak_SameUid_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await useCase.LimitBreakAsync("01A", "01A");
                Assert.Fail();
            }
            catch (ArgumentException)
            {
            }

            Assert.IsEmpty(service.Calls);
        });

        [UnityTest]
        public IEnumerator LevelUp_HandlesCodes() => UniTask.ToCoroutine(async () =>
        {
            service.LevelUpResults.Enqueue(FakeSupportService.Ok(new LevelUpResponse(4, 900, 950)));

            await useCase.LevelUpAsync("01A", 4);

            AssertHandled("uta/SUPPORT_INVALID_TARGET_LEVEL", "uta/SUPPORT_NOT_ENOUGH_POINTS", "uta/PLAYER_NOT_ENOUGH_GOLD", "uta/SUPPORT_NOT_OWNED");
        });

        [UnityTest]
        public IEnumerator LimitBreak_HandlesCodes() => UniTask.ToCoroutine(async () =>
        {
            service.LimitBreakResults.Enqueue(FakeSupportService.Ok(new LimitBreakResponse(1, 45)));

            await useCase.LimitBreakAsync("01A", "01D");

            AssertHandled("uta/SUPPORT_MAX_LIMIT_BREAK", "uta/SUPPORT_INVALID_MATERIAL", "uta/SUPPORT_NOT_OWNED");
        });

        private void AssertHandled(params string[] expected)
        {
            Assert.IsNotNull(service.LastHeaders);
            foreach (var reason in expected)
            {
                var found = false;
                foreach (var entry in service.LastHeaders)
                {
                    if (entry.Value != null && entry.Value.Contains(reason))
                    {
                        found = true;
                    }
                }

                Assert.IsTrue(found, reason);
            }
        }
    }
}

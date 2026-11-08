using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using MagicOnion;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Gacha;
using Uta.Client.Infrastructure.Gacha;
using Uta.Client.Infrastructure.Player;
using Uta.Shared.Contracts.Gacha;

namespace Uta.Client.Tests
{
    /// <summary>手順のテスト。偽の IGachaService・本物の PlayerRepository・偽の IPlayerUseCase。</summary>
    public sealed class GachaUseCaseTests
    {
        private FakeGachaService service;
        private PlayerRepository players;
        private FakePlayerUseCase player;
        private GachaUseCase useCase;

        [SetUp]
        public void SetUp()
        {
            service = new FakeGachaService();
            players = new PlayerRepository();
            players.Upsert(GachaFixtures.Player(GachaFixtures.Currency));
            player = new FakePlayerUseCase();
            useCase = new GachaUseCase(service, players, player, GachaFixtures.CreateMaster());
        }

        private static Func<UnaryResult<DrawResponse>> Success()
        {
            return FakeGachaService.Ok(new DrawResponse(new[] { GachaFixtures.IdolResult(1, 1, GachaOutcome.New, 0) }, 3000, 1));
        }

        [UnityTest]
        public IEnumerator Draw_Success_RefreshesAndReturnsResult() => UniTask.ToCoroutine(async () =>
        {
            service.DrawResults.Enqueue(Success());

            var result = await useCase.DrawAsync(1, 1);

            Assert.AreEqual(DrawResultKind.Succeeded, result.Kind);
            Assert.AreEqual(1, result.Results.Count);
            Assert.AreEqual(3000, result.GachaCurrency);
            Assert.AreEqual(1, result.ExchangePoints);
            Assert.AreEqual(1, player.RefreshCount);
        });

        [UnityTest]
        public IEnumerator Draw_RefreshFails_StillSucceeds() => UniTask.ToCoroutine(async () =>
        {
            service.DrawResults.Enqueue(Success());
            player.RefreshResults.Enqueue(() => throw new InvalidOperationException("sign-in failed: x"));

            var result = await useCase.DrawAsync(1, 1);

            Assert.AreEqual(DrawResultKind.Succeeded, result.Kind);
        });

        [UnityTest]
        public IEnumerator Draw_NotEnoughCurrencyInRepository_DoesNotCall() => UniTask.ToCoroutine(async () =>
        {
            players.Upsert(GachaFixtures.Player(100));

            var result = await useCase.DrawAsync(1, 1);

            Assert.AreEqual(DrawResultKind.NotEnoughCurrency, result.Kind);
            Assert.IsEmpty(service.Calls);
        });

        [UnityTest]
        public IEnumerator Draw_NoPlayer_ReturnsNotEnoughCurrency() => UniTask.ToCoroutine(async () =>
        {
            var empty = new GachaUseCase(service, new PlayerRepository(), player, GachaFixtures.CreateMaster());

            var result = await empty.DrawAsync(1, 1);

            Assert.AreEqual(DrawResultKind.NotEnoughCurrency, result.Kind);
            Assert.IsEmpty(service.Calls);
        });

        [UnityTest]
        public IEnumerator Draw_ServerNotEnoughCurrency_DoesNotRefresh() => UniTask.ToCoroutine(async () =>
        {
            service.DrawResults.Enqueue(FakeGachaService.Fail<DrawResponse>(GachaErrors.NotEnoughCurrency));

            var result = await useCase.DrawAsync(1, 1);

            Assert.AreEqual(DrawResultKind.NotEnoughCurrency, result.Kind);
            Assert.AreEqual(0, player.RefreshCount);
        });

        [UnityTest]
        public IEnumerator Draw_OtherError_ReturnsFailed() => UniTask.ToCoroutine(async () =>
        {
            service.DrawResults.Enqueue(FakeGachaService.Fail<DrawResponse>(GachaErrors.CostMismatch));

            var result = await useCase.DrawAsync(1, 1);

            Assert.AreEqual(DrawResultKind.Failed, result.Kind);
            Assert.AreEqual(0, player.RefreshCount);
        });

        [UnityTest]
        public IEnumerator Draw_InvalidCount_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await useCase.DrawAsync(1, 2);
                Assert.Fail("例外が投げられなかった");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            Assert.IsEmpty(service.Calls);
        });

        [UnityTest]
        public IEnumerator Draw_PassesExpectedCost() => UniTask.ToCoroutine(async () =>
        {
            service.DrawResults.Enqueue(Success());
            service.DrawResults.Enqueue(Success());

            await useCase.DrawAsync(1, 1);
            await useCase.DrawAsync(1, 10);

            CollectionAssert.AreEqual(new[] { "Draw:1,1,250", "Draw:1,10,2500" }, service.Calls);
        });

        [UnityTest]
        public IEnumerator GetExchangePoints_FoundNotFoundAndFailure() => UniTask.ToCoroutine(async () =>
        {
            var response = new GetGachasResponse(new[] { new GachaStatusDto(1, 37) });
            service.GetGachasResults.Enqueue(FakeGachaService.Ok(response));
            service.GetGachasResults.Enqueue(FakeGachaService.Ok(response));
            service.GetGachasResults.Enqueue(FakeGachaService.Fail<GetGachasResponse>(GachaErrors.NotFound));

            Assert.AreEqual(37, await useCase.GetExchangePointsAsync(1));
            Assert.AreEqual(0, await useCase.GetExchangePointsAsync(2));
            Assert.IsNull(await useCase.GetExchangePointsAsync(1));
        });
    }
}

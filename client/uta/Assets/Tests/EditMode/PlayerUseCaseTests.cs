using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Player;
using Uta.Client.Infrastructure.Idol;
using Uta.Client.Infrastructure.Player;
using Uta.Client.Infrastructure.SupportCard;
using Uta.Shared.Contracts.Player;

namespace Uta.Client.Tests
{
    /// <summary>手順のテスト（ADR 0022）。偽の IPlayerService とメモリのリポジトリで、サインアップ・サインインの場合分けを確かめる。</summary>
    public sealed class PlayerUseCaseTests
    {
        private FakePlayerService service;
        private PlayerRepository players;
        private IdolRepository idols;
        private SupportCardRepository supportCards;
        private DiscardNoticeState notice;
        private PlayerUseCase useCase;

        [SetUp]
        public void SetUp()
        {
            service = new FakePlayerService();
            players = new PlayerRepository();
            idols = new IdolRepository();
            supportCards = new SupportCardRepository();
            notice = new DiscardNoticeState();
            useCase = new PlayerUseCase(service, players, idols, supportCards, notice);
        }

        private static SignInResponse SignIn(int level = 1, OwnedIdolDto[] idols = null, OwnedSupportCardDto[] supportCards = null)
        {
            return new SignInResponse(level, 2, 3, 4, 5, false, false, idols, supportCards, "P1");
        }

        private static void StartsWith(string prefix, Exception e)
        {
            Assert.IsNotNull(e, "例外が投げられなかった");
            Assert.IsInstanceOf<InvalidOperationException>(e);
            StringAssert.StartsWith(prefix, e.Message);
        }

        private static async UniTask<Exception> CatchAsync(Func<UniTask> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        [UnityTest]
        public IEnumerator Enter_AfterRegistration_CallsSignUpThenSignIn() => UniTask.ToCoroutine(async () =>
        {
            service.SignUpResults.Enqueue(FakePlayerService.Ok(new SignUpResponse(true)));
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn()));

            var result = await useCase.EnterAsync(true);

            CollectionAssert.AreEqual(new[] { "SignUp", "SignIn" }, service.Calls);
            Assert.IsTrue(result.SignedUp);
            Assert.IsTrue(result.Created);
        });

        [UnityTest]
        public IEnumerator Enter_Registered_CallsOnlySignIn() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn()));

            var result = await useCase.EnterAsync(false);

            CollectionAssert.AreEqual(new[] { "SignIn" }, service.Calls);
            Assert.IsFalse(result.SignedUp);
            Assert.IsFalse(result.Created);
        });

        [UnityTest]
        public IEnumerator Enter_NotCreated_SignsUpThenSignsInAgain() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotCreated));
            service.SignUpResults.Enqueue(FakePlayerService.Ok(new SignUpResponse(true)));
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn()));

            var result = await useCase.EnterAsync(false);

            CollectionAssert.AreEqual(new[] { "SignIn", "SignUp", "SignIn" }, service.Calls);
            Assert.IsTrue(result.SignedUp);
            Assert.IsTrue(result.Created);
        });

        [UnityTest]
        public IEnumerator Enter_NotCreatedAfterSignUp_Throws() => UniTask.ToCoroutine(async () =>
        {
            service.SignUpResults.Enqueue(FakePlayerService.Ok(new SignUpResponse(true)));
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotCreated));

            var e = await CatchAsync(() => useCase.EnterAsync(true));

            StartsWith("sign-in: player not created after sign-up", e);
        });

        [UnityTest]
        public IEnumerator Enter_NotCreatedTwice_Throws() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotCreated));
            service.SignUpResults.Enqueue(FakePlayerService.Ok(new SignUpResponse(true)));
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotCreated));

            var e = await CatchAsync(() => useCase.EnterAsync(false));

            StartsWith("sign-in: player not created after sign-up", e);
            CollectionAssert.AreEqual(new[] { "SignIn", "SignUp", "SignIn" }, service.Calls);
        });

        [UnityTest]
        public IEnumerator Enter_SignUpFails_ThrowsWithoutSignIn() => UniTask.ToCoroutine(async () =>
        {
            service.SignUpResults.Enqueue(FakePlayerService.Fail<SignUpResponse>(PlayerErrors.NotEnoughGold));

            var e = await CatchAsync(() => useCase.EnterAsync(true));

            StartsWith("sign-up failed: ", e);
            CollectionAssert.AreEqual(new[] { "SignUp" }, service.Calls);
        });

        [UnityTest]
        public IEnumerator Enter_SignInFails_Throws() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotEnoughGold));

            var e = await CatchAsync(() => useCase.EnterAsync(false));

            StartsWith("sign-in failed: ", e);
        });

        [UnityTest]
        public IEnumerator Enter_Success_ReplacesRepositories() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                7,
                new[] { new OwnedIdolDto(9, 1, 2, 3, "U9"), new OwnedIdolDto(4, 5, 6, 7, "U4") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12) })));

            var result = await useCase.EnterAsync(false);

            Assert.AreEqual(7, players.Find().Level);
            Assert.AreEqual(7, result.Player.Level);
            Assert.AreEqual(2, result.IdolCount);
            Assert.AreEqual(1, result.SupportCardCount);
            var all = idols.FindAll();
            Assert.AreEqual(2, all.Count);
            Assert.AreEqual(4, all[0].MasterId);
            Assert.AreEqual(9, all[1].MasterId);
            Assert.AreEqual(1, supportCards.FindAll().Count);
            Assert.AreEqual(10, supportCards.Find("01ARZ3NDEKTSV4RRFFQ69G5FAV").MasterId);
        });

        [UnityTest]
        public IEnumerator Enter_Failed_ThrowsAndKeepsRepositories() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                1,
                new[] { new OwnedIdolDto(1, 0, 0, 0, "U1") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12) })));
            await useCase.EnterAsync(false);
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotEnoughGold));

            var e = await CatchAsync(() => useCase.EnterAsync(false));

            StartsWith("sign-in failed: ", e);
            Assert.AreEqual(1, players.Find().Level);
            Assert.AreEqual(1, idols.FindAll().Count);
            Assert.AreEqual(1, supportCards.FindAll().Count);
        });

        [UnityTest]
        public IEnumerator Refresh_Success_ReplacesRepositories() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(1, new[] { new OwnedIdolDto(1, 0, 0, 0, "U1") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FB0", 20, 1, 0) })));
            await useCase.EnterAsync(false);
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                2,
                new[] { new OwnedIdolDto(2, 0, 0, 0, "U2"), new OwnedIdolDto(3, 0, 0, 0, "U3") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12) })));

            await useCase.RefreshAsync();

            Assert.AreEqual(2, players.Find().Level);
            Assert.AreEqual(2, idols.FindAll().Count);
            Assert.IsNull(idols.Find("U1"));
            Assert.AreEqual(1, supportCards.FindAll().Count);
            Assert.IsNull(supportCards.Find("01ARZ3NDEKTSV4RRFFQ69G5FB0"));
        });

        [UnityTest]
        public IEnumerator Refresh_Failed_ThrowsAndKeepsRepositories() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                1,
                new[] { new OwnedIdolDto(1, 0, 0, 0, "U1") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12) })));
            await useCase.EnterAsync(false);
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotEnoughGold));

            var e = await CatchAsync(() => useCase.RefreshAsync());

            StartsWith("sign-in failed: ", e);
            Assert.AreEqual(1, players.Find().Level);
            Assert.AreEqual(1, idols.FindAll().Count);
            Assert.AreEqual(1, supportCards.FindAll().Count);
        });

        [UnityTest]
        public IEnumerator Refresh_NullIdInResponse_ThrowsAndKeepsRepositories() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                1,
                new[] { new OwnedIdolDto(1, 0, 0, 0, "U1") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12) })));
            await useCase.EnterAsync(false);
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                5,
                new[] { new OwnedIdolDto(2, 0, 0, 0, "U2"), new OwnedIdolDto(1, 0, 0, 0, null) },
                null)));

            var e = await CatchAsync(() => useCase.RefreshAsync());

            Assert.IsInstanceOf<ArgumentNullException>(e);
            Assert.AreEqual(1, players.Find().Level);
            Assert.AreEqual(1, idols.FindAll().Count);
            Assert.IsNotNull(idols.Find("U1"));
            Assert.AreEqual(1, supportCards.FindAll().Count);
        });

        [UnityTest]
        public IEnumerator Refresh_NotCreated_ThrowsAndKeepsRepositories() => UniTask.ToCoroutine(async () =>
        {
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn(
                1,
                new[] { new OwnedIdolDto(1, 0, 0, 0, "U1") },
                new[] { new OwnedSupportCardDto("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10, 11, 12) })));
            await useCase.EnterAsync(false);
            service.SignInResults.Enqueue(FakePlayerService.Fail<SignInResponse>(PlayerErrors.NotCreated));

            var e = await CatchAsync(() => useCase.RefreshAsync());

            Assert.IsInstanceOf<InvalidOperationException>(e);
            Assert.AreEqual("sign-in failed: player not created", e.Message);
            Assert.AreEqual(1, players.Find().Level);
            Assert.AreEqual(1, idols.FindAll().Count);
            Assert.AreEqual(1, supportCards.FindAll().Count);
        });

        [UnityTest]
        public IEnumerator Enter_Success_ResetsDiscardNotice() => UniTask.ToCoroutine(async () =>
        {
            notice.MarkShown();
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn()));

            await useCase.EnterAsync(false);

            Assert.IsFalse(notice.Shown);
        });

        [UnityTest]
        public IEnumerator Refresh_Success_ResetsDiscardNotice() => UniTask.ToCoroutine(async () =>
        {
            notice.MarkShown();
            service.SignInResults.Enqueue(FakePlayerService.Ok(SignIn()));

            await useCase.RefreshAsync();

            Assert.IsFalse(notice.Shown);
        });

        [Test]
        public void MarkDiscardNoticeShown_SetsFlag()
        {
            useCase.MarkDiscardNoticeShown();

            Assert.IsTrue(notice.Shown);
        }
    }
}

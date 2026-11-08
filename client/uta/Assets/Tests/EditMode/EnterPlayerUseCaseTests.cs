using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Uta.Client.Application.Player;

namespace Uta.Client.Tests
{
    public sealed class EnterPlayerUseCaseTests
    {
        private static PlayerSnapshot NewSnapshot(int level)
        {
            return new PlayerSnapshot(level, 0, 0, 0, 0, false, false, new List<OwnedIdol>(), new List<OwnedSupportCard>());
        }

        private sealed class FakePlayerPort : IPlayerPort
        {
            public readonly Queue<PlayerSignInResult> SignInResults = new Queue<PlayerSignInResult>();
            public readonly Queue<PlayerSignUpResult> SignUpResults = new Queue<PlayerSignUpResult>();
            public readonly List<string> Calls = new List<string>();

            public UniTask<PlayerSignUpResult> SignUpAsync()
            {
                Calls.Add("SignUp");
                return UniTask.FromResult(SignUpResults.Dequeue());
            }

            public UniTask<PlayerSignInResult> SignInAsync()
            {
                Calls.Add("SignIn");
                return UniTask.FromResult(SignInResults.Dequeue());
            }
        }

        [Test]
        public void Execute_AfterRegistration_CallsSignUpThenSignIn()
        {
            var port = new FakePlayerPort();
            port.SignUpResults.Enqueue(PlayerSignUpResult.Success(true));
            port.SignInResults.Enqueue(PlayerSignInResult.Success(NewSnapshot(1)));
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var result = useCase.ExecuteAsync(true).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "SignUp", "SignIn" }, port.Calls);
            Assert.IsTrue(result.SignedUp);
            Assert.IsTrue(result.Created);
        }

        [Test]
        public void Execute_Registered_CallsOnlySignIn()
        {
            var port = new FakePlayerPort();
            port.SignInResults.Enqueue(PlayerSignInResult.Success(NewSnapshot(1)));
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var result = useCase.ExecuteAsync(false).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "SignIn" }, port.Calls);
            Assert.IsFalse(result.SignedUp);
            Assert.IsFalse(result.Created);
        }

        [Test]
        public void Execute_NotCreated_SignsUpThenSignsInAgain()
        {
            var port = new FakePlayerPort();
            port.SignInResults.Enqueue(PlayerSignInResult.NotCreated());
            port.SignUpResults.Enqueue(PlayerSignUpResult.Success(true));
            port.SignInResults.Enqueue(PlayerSignInResult.Success(NewSnapshot(1)));
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var result = useCase.ExecuteAsync(false).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "SignIn", "SignUp", "SignIn" }, port.Calls);
            Assert.IsTrue(result.SignedUp);
            Assert.IsTrue(result.Created);
        }

        [Test]
        public void Execute_NotCreatedAfterSignUp_Throws()
        {
            var port = new FakePlayerPort();
            port.SignUpResults.Enqueue(PlayerSignUpResult.Success(true));
            port.SignInResults.Enqueue(PlayerSignInResult.NotCreated());
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var ex = Assert.Throws<InvalidOperationException>(() => useCase.ExecuteAsync(true).GetAwaiter().GetResult());

            Assert.AreEqual("sign-in: player not created after sign-up", ex.Message);
        }

        [Test]
        public void Execute_NotCreatedTwice_Throws()
        {
            var port = new FakePlayerPort();
            port.SignInResults.Enqueue(PlayerSignInResult.NotCreated());
            port.SignUpResults.Enqueue(PlayerSignUpResult.Success(true));
            port.SignInResults.Enqueue(PlayerSignInResult.NotCreated());
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var ex = Assert.Throws<InvalidOperationException>(() => useCase.ExecuteAsync(false).GetAwaiter().GetResult());

            Assert.AreEqual("sign-in: player not created after sign-up", ex.Message);
            CollectionAssert.AreEqual(new[] { "SignIn", "SignUp", "SignIn" }, port.Calls);
        }

        [Test]
        public void Execute_SignUpFails_ThrowsWithoutSignIn()
        {
            var port = new FakePlayerPort();
            port.SignUpResults.Enqueue(PlayerSignUpResult.Failure("boom"));
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var ex = Assert.Throws<InvalidOperationException>(() => useCase.ExecuteAsync(true).GetAwaiter().GetResult());

            Assert.AreEqual("sign-up failed: boom", ex.Message);
            CollectionAssert.AreEqual(new[] { "SignUp" }, port.Calls);
        }

        [Test]
        public void Execute_SignInFails_Throws()
        {
            var port = new FakePlayerPort();
            port.SignInResults.Enqueue(PlayerSignInResult.Failure("bad"));
            var useCase = new EnterPlayerUseCase(port, new PlayerCache());

            var ex = Assert.Throws<InvalidOperationException>(() => useCase.ExecuteAsync(false).GetAwaiter().GetResult());

            Assert.AreEqual("sign-in failed: bad", ex.Message);
        }

        [Test]
        public void Execute_Success_SetsCache()
        {
            var port = new FakePlayerPort();
            var snapshot = NewSnapshot(7);
            port.SignInResults.Enqueue(PlayerSignInResult.Success(snapshot));
            var cache = new PlayerCache();
            var useCase = new EnterPlayerUseCase(port, cache);

            var result = useCase.ExecuteAsync(false).GetAwaiter().GetResult();

            Assert.AreSame(snapshot, cache.Current);
            Assert.AreSame(snapshot, result.Snapshot);
        }

        [Test]
        public void Execute_Failure_KeepsCache()
        {
            var port = new FakePlayerPort();
            port.SignInResults.Enqueue(PlayerSignInResult.Failure("bad"));
            var cache = new PlayerCache();
            var previous = NewSnapshot(3);
            cache.Set(previous);
            var useCase = new EnterPlayerUseCase(port, cache);

            Assert.Throws<InvalidOperationException>(() => useCase.ExecuteAsync(false).GetAwaiter().GetResult());

            Assert.AreSame(previous, cache.Current);
        }
    }
}

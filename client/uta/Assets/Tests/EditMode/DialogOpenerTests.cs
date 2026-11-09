using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Uta.Client.Presentation.Common;
using DialogResult = Uta.Client.Presentation.Common.DialogResult;

namespace Uta.Client.Tests
{
    /// <summary>FakeScreenNavigator で DialogOpener を確かめる。どれも同期の [Test]（戻り値の UniTask は await せず Status で見る）。</summary>
    public sealed class DialogOpenerTests
    {
        private CallLog log;
        private FakeScreenNavigator navigator;
        private DialogOpener opener;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            navigator = new FakeScreenNavigator(log);
            opener = new DialogOpener(navigator);
        }

        private DialogArgs ArgsAt(int index)
        {
            return (DialogArgs)navigator.Args[index];
        }

        [Test]
        public void ShowInfo_PushesOverlayWithOk()
        {
            var task = opener.ShowInfoAsync("m");

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.That(log.Entries, Is.EqualTo(new[] { "PushOverlay:DialogPresenter", "WaitForPop" }));
            Assert.AreEqual("m", ArgsAt(0).Message);
            Assert.That(ArgsAt(0).Labels, Is.EqualTo(new[] { "OK" }));
            Assert.That(ArgsAt(0).Colors, Is.EqualTo(new[] { UiTheme.ButtonPrimary }));
        }

        [Test]
        public void Confirm_Labels_AndResult()
        {
            navigator.PopResults.Enqueue(0);
            navigator.PopResults.Enqueue(1);

            var positive = opener.ConfirmAsync("m", "y", "n");
            var negative = opener.ConfirmAsync("m", "y", "n");

            Assert.AreEqual(DialogResult.Positive, positive.GetAwaiter().GetResult());
            Assert.AreEqual(DialogResult.Negative, negative.GetAwaiter().GetResult());
            Assert.That(ArgsAt(0).Labels, Is.EqualTo(new[] { "y", "n" }));
            Assert.That(ArgsAt(0).Colors, Is.EqualTo(new[] { UiTheme.ButtonPrimary, UiTheme.ButtonSecondary }));
        }

        [Test]
        public void Choose_ReturnsIndex()
        {
            navigator.PopResults.Enqueue(2);

            var task = opener.ChooseAsync("m", new[] { "a", "b", "c" });

            Assert.AreEqual(2, task.GetAwaiter().GetResult());
            Assert.That(ArgsAt(0).Labels, Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(ArgsAt(0).Colors, Is.EqualTo(new[] { UiTheme.ButtonPrimary, UiTheme.ButtonPrimary, UiTheme.ButtonPrimary }));
        }

        [Test]
        public void Choose_InvalidLabels_Throws()
        {
            Assert.Throws<ArgumentException>(() => opener.ChooseAsync("m", null));
            Assert.Throws<ArgumentException>(() => opener.ChooseAsync("m", new string[0]));
            Assert.Throws<ArgumentException>(() => opener.ChooseAsync("m", new[] { "a", "b", "c", "d", "e" }));
            Assert.That(log.Entries, Is.Empty);
        }

        [Test]
        public void Queue_SecondWaitsForFirst()
        {
            var gate = new UniTaskCompletionSource<object>();
            navigator.PopGates.Enqueue(gate);

            var first = opener.ShowInfoAsync("a");
            var second = opener.ShowInfoAsync("b");

            Assert.AreEqual(UniTaskStatus.Pending, first.Status);
            Assert.AreEqual(UniTaskStatus.Pending, second.Status);
            Assert.AreEqual(1, navigator.Args.Count);
            Assert.AreEqual("a", ArgsAt(0).Message);

            gate.TrySetResult(0);

            Assert.AreEqual(UniTaskStatus.Succeeded, first.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, second.Status);
            Assert.AreEqual(2, navigator.Args.Count);
            Assert.AreEqual("b", ArgsAt(1).Message);
        }

        [Test]
        public void Result_NotInt_ReturnsMinusOne()
        {
            navigator.PopResults.Enqueue(null);

            var task = opener.ChooseAsync("m", new[] { "a" });

            Assert.AreEqual(-1, task.GetAwaiter().GetResult());
        }

        [Test]
        public void NullNavigator_Throws()
        {
            var task = new DialogOpener(null).ShowInfoAsync("m");

            Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
            Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult());
        }

        [Test]
        public void Disposed_QueuedRequest_ReturnsMinusOneWithoutPush()
        {
            var gate = new UniTaskCompletionSource<object>();
            navigator.PopGates.Enqueue(gate);
            var first = opener.ChooseAsync("a", new[] { "x" });
            var second = opener.ChooseAsync("b", new[] { "x" });

            opener.Dispose();
            gate.TrySetResult(0);

            Assert.AreEqual(0, first.GetAwaiter().GetResult());
            Assert.AreEqual(-1, second.GetAwaiter().GetResult());
            Assert.AreEqual(1, navigator.Args.Count);
        }

        [Test]
        public void PushThrows_AfterDispose_ReturnsMinusOne()
        {
            navigator.OnPushOverlay = () => opener.Dispose();
            navigator.PushOverlayException = new InvalidOperationException("scope disposed");

            var task = opener.ChooseAsync("m", new[] { "x" });

            Assert.AreEqual(-1, task.GetAwaiter().GetResult());
        }
    }
}

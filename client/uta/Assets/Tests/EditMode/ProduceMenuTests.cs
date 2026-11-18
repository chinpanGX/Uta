using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Tests
{
    public sealed class ProduceMenuTests
    {
        private const string ChooseEntry = "Choose:メニュー:ホームへ戻る（あとで再開できます）/プロデュースを放棄する/閉じる";
        private const string ConfirmEntry = "Confirm:放棄すると報酬は受け取れません。放棄しますか？";

        private CallLog log;
        private FakeDialogOpener dialogs;
        private FakeProduceUseCase produce;
        private ProduceMenu menu;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            dialogs = new FakeDialogOpener(log);
            produce = new FakeProduceUseCase(log);
            var flow = new ProduceFlow(new ProduceSessionState(), new FakeScreenNavigator(log), new PageStack(() => 1, () => 0), dialogs, new FakeSceneNavigator(log));
            menu = new ProduceMenu(dialogs, produce, flow);
        }

        [UnityTest]
        public IEnumerator Show_GoHome_ExitsWithoutAbandon() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ChooseResults.Enqueue(0);

            await menu.ShowAsync();

            CollectionAssert.AreEqual(new[] { ChooseEntry, "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Show_AbandonConfirmed_AbandonsThenExits() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ChooseResults.Enqueue(1);
            dialogs.ConfirmResults.Enqueue(DialogResult.Positive);

            await menu.ShowAsync();

            CollectionAssert.AreEqual(new[] { ChooseEntry, ConfirmEntry, "Abandon", "GoToHome" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Show_AbandonDeclined_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ChooseResults.Enqueue(1);
            dialogs.ConfirmResults.Enqueue(DialogResult.Negative);

            await menu.ShowAsync();

            CollectionAssert.AreEqual(new[] { ChooseEntry, ConfirmEntry }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Show_AbandonFailed_DoesNotExit() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ChooseResults.Enqueue(1);
            produce.AbandonResults.Enqueue(() => UniTask.FromResult(false));

            await menu.ShowAsync();

            CollectionAssert.AreEqual(new[] { ChooseEntry, ConfirmEntry, "Abandon" }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Show_Close_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ChooseResults.Enqueue(2);

            await menu.ShowAsync();

            CollectionAssert.AreEqual(new[] { ChooseEntry }, log.Entries);
        });

        [UnityTest]
        public IEnumerator Show_DialogDismissed_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            dialogs.ChooseResults.Enqueue(-1);

            await menu.ShowAsync();

            CollectionAssert.AreEqual(new[] { ChooseEntry }, log.Entries);
        });
    }
}

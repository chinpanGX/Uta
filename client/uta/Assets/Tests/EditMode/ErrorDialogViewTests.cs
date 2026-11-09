using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using Uta.Client.Bootstrap;
using Uta.Client.Presentation.Common;
using VolTech.Client;

namespace Uta.Client.Tests
{
    public sealed class ErrorDialogViewTests
    {
        [UnityTest]
        public IEnumerator AutoDismiss_ReturnsFallback() => UniTask.ToCoroutine(async () =>
        {
            {
                var view = new ErrorDialogView(true, new ActiveSceneUi());

                Assert.AreEqual(ErrorDialogButton.Ok, await view.ShowAsync(null, ErrorDialogButtons.Ok));
                Assert.AreEqual(ErrorDialogButton.Title, await view.ShowAsync(null, ErrorDialogButtons.RetryOrTitle));
                Assert.AreEqual(ErrorDialogButton.Home, await view.ShowAsync(null, ErrorDialogButtons.Home));
                Assert.AreEqual(ErrorDialogButton.Title, await view.ShowAsync(null, ErrorDialogButtons.Title));
                Assert.AreEqual(ErrorDialogButton.Store, await view.ShowAsync(null, ErrorDialogButtons.Store));
            }
        });

        [UnityTest]
        public IEnumerator NoSceneUi_ReturnsFallback() => UniTask.ToCoroutine(async () =>
        {
            {
                var view = new ErrorDialogView(false, new ActiveSceneUi());

                Assert.AreEqual(ErrorDialogButton.Title, await view.ShowAsync(null, ErrorDialogButtons.RetryOrTitle));
            }
        });

        [UnityTest]
        public IEnumerator RetryOrTitle_SecondClicked_ReturnsTitle() => UniTask.ToCoroutine(async () =>
        {
            {
                var log = new CallLog();
                var dialogs = new FakeDialogOpener(log);
                dialogs.ChooseResults.Enqueue(1);
                var sceneUi = AttachedSceneUi(dialogs);
                var view = new ErrorDialogView(false, sceneUi);

                var result = await view.ShowAsync(null, ErrorDialogButtons.RetryOrTitle);

                Assert.AreEqual(ErrorDialogButton.Title, result);
                CollectionAssert.AreEqual(new[] { "Choose:エラーが発生しました。\n:再試行/タイトルへ" }, log.Entries);
                DestroyRoot(sceneUi);
            }
        });

        [UnityTest]
        public IEnumerator Dismissed_ReturnsFallback() => UniTask.ToCoroutine(async () =>
        {
            {
                var dialogs = new FakeDialogOpener(new CallLog());
                dialogs.ChooseResults.Enqueue(-1);
                var sceneUi = AttachedSceneUi(dialogs);
                var view = new ErrorDialogView(false, sceneUi);

                var result = await view.ShowAsync(null, ErrorDialogButtons.RetryOrTitle);

                Assert.AreEqual(ErrorDialogButton.Title, result);
                DestroyRoot(sceneUi);
            }
        });

        private static ActiveSceneUi AttachedSceneUi(DialogOpener dialogs)
        {
            var root = new UnityEngine.GameObject("Root").AddComponent<UiRoot>();
            var sceneUi = new ActiveSceneUi();
            sceneUi.Attach(root, dialogs);
            return sceneUi;
        }

        private static void DestroyRoot(ActiveSceneUi sceneUi)
        {
            UnityEngine.Object.DestroyImmediate(sceneUi.Root.gameObject);
        }
    }
}

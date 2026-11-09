using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;
using Object = UnityEngine.Object;

namespace Uta.Client.Tests
{
    public sealed class DialogPresenterTests
    {
        private CallLog log;
        private DialogView view;
        private DialogPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            log = new CallLog();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Parts/Dialog.prefab");
            view = Object.Instantiate(prefab).GetComponent<DialogView>();
            var args = new DialogArgs(
                "message",
                new[] { "first", "second" },
                new[] { UiTheme.ButtonPrimary, UiTheme.ButtonSecondary });
            presenter = new DialogPresenter(view, args, new FakeScreenNavigator(log));
            presenter.InitializeAsync();
        }

        [TearDown]
        public void TearDown()
        {
            if (view != null)
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void Initialize_SetsMessageAndButtons()
        {
            var texts = view.GetComponentsInChildren<TMP_Text>();
            var message = string.Empty;
            foreach (var text in texts)
            {
                if (text.gameObject.name == "MessageText")
                {
                    message = text.text;
                }
            }

            var buttons = view.GetComponentsInChildren<Button>();

            Assert.AreEqual("message", message);
            Assert.AreEqual(2, buttons.Length);
            Assert.AreEqual("Button0", buttons[0].gameObject.name);
            Assert.AreEqual("Button1", buttons[1].gameObject.name);
            Assert.AreEqual("first", buttons[0].GetComponentInChildren<TMP_Text>().text);
            Assert.AreEqual("second", buttons[1].GetComponentInChildren<TMP_Text>().text);
        }

        [Test]
        public void Click_PopsOnce()
        {
            var buttons = view.GetComponentsInChildren<Button>();

            buttons[0].onClick.Invoke();
            buttons[1].onClick.Invoke();
            buttons[0].onClick.Invoke();

            Assert.That(log.Entries, Is.EqualTo(new[] { "PopOverlay:DialogPresenter" }));
        }

        [Test]
        public void Complete_ReturnsClickedIndex()
        {
            view.GetComponentsInChildren<Button>()[1].onClick.Invoke();

            Assert.AreEqual(1, presenter.CompleteAsync().GetAwaiter().GetResult());
        }

        [Test]
        public void Complete_NotClicked_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, presenter.CompleteAsync().GetAwaiter().GetResult());
        }
    }
}

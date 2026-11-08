using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;
using DialogResult = Uta.Client.Presentation.Common.DialogResult;
using Object = UnityEngine.Object;

namespace Uta.Client.Tests
{
    /// <summary>どれも同期の [Test]。戻り値の UniTask は await せずに変数に受け、層の子とボタンをその場で確かめる（待ち行列が空なら、ダイアログは呼んだその場で出る）。</summary>
    public sealed class DialogOpenerTests
    {
        private RectTransform layer;
        private DialogOpener opener;

        [SetUp]
        public void SetUp()
        {
            layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Parts/Dialog.prefab").GetComponent<DialogView>();
            opener = new DialogOpener(layer, prefab);
        }

        [TearDown]
        public void TearDown()
        {
            if (layer != null)
            {
                Object.DestroyImmediate(layer.gameObject);
            }
        }

        private Button[] Buttons()
        {
            return layer.GetComponentsInChildren<Button>();
        }

        private string MessageText()
        {
            foreach (var text in layer.GetComponentsInChildren<TMP_Text>())
            {
                if (text.gameObject.name == "MessageText")
                {
                    return text.text;
                }
            }

            return null;
        }

        [Test]
        public void ConfirmAsync_PositiveClicked_ReturnsPositive()
        {
            var task = opener.ConfirmAsync("m");
            Assert.AreEqual(1, layer.childCount);

            Buttons()[0].onClick.Invoke();

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(DialogResult.Positive, task.GetAwaiter().GetResult());
            Assert.AreEqual(0, layer.childCount);
        }

        [Test]
        public void ConfirmAsync_NegativeClicked_ReturnsNegative()
        {
            var task = opener.ConfirmAsync("m");

            Buttons()[1].onClick.Invoke();

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(DialogResult.Negative, task.GetAwaiter().GetResult());
            Assert.AreEqual(0, layer.childCount);
        }

        [Test]
        public void ChooseAsync_SecondClicked_ReturnsOne()
        {
            var task = opener.ChooseAsync("m", new[] { "a", "b", "c" });
            Assert.AreEqual(3, Buttons().Length);

            Buttons()[1].onClick.Invoke();

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(1, task.GetAwaiter().GetResult());
        }

        [Test]
        public void ChooseAsync_FiveLabels_Throws()
        {
            Assert.Throws<ArgumentException>(() => opener.ChooseAsync("m", new[] { "a", "b", "c", "d", "e" }));
            Assert.AreEqual(0, layer.childCount);
        }

        [Test]
        public void ShowInfoAsync_Twice_ShowsOneAtATime()
        {
            var first = opener.ShowInfoAsync("a");
            var second = opener.ShowInfoAsync("b");

            Assert.AreEqual(1, layer.childCount);
            Assert.AreEqual("a", MessageText());
            Assert.AreEqual(UniTaskStatus.Pending, second.Status);

            Buttons()[0].onClick.Invoke();

            Assert.AreEqual(UniTaskStatus.Succeeded, first.Status);
            Assert.AreEqual(1, layer.childCount);
            Assert.AreEqual("b", MessageText());

            Buttons()[0].onClick.Invoke();

            Assert.AreEqual(UniTaskStatus.Succeeded, second.Status);
            Assert.AreEqual(0, layer.childCount);
        }

        [Test]
        public void LayerDestroyed_ReturnsWithoutDialog()
        {
            Object.DestroyImmediate(layer.gameObject);

            var confirm = opener.ConfirmAsync("m");
            var choose = opener.ChooseAsync("m", new[] { "a" });
            var info = opener.ShowInfoAsync("m");

            Assert.AreEqual(UniTaskStatus.Succeeded, confirm.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, choose.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, info.Status);
            Assert.AreEqual(DialogResult.Negative, confirm.GetAwaiter().GetResult());
            Assert.AreEqual(-1, choose.GetAwaiter().GetResult());
        }

        [Test]
        public void ButtonColors_PrimaryThenSecondary()
        {
            var task = opener.ConfirmAsync("m");
            var buttons = Buttons();

            Assert.AreEqual(UiTheme.ButtonPrimary, buttons[0].GetComponent<ButtonView>().GetComponent<Image>().color);
            Assert.AreEqual(UiTheme.ButtonSecondary, buttons[1].GetComponent<ButtonView>().GetComponent<Image>().color);

            buttons[0].onClick.Invoke();
            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
        }
    }
}

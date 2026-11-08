using System;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Home;

namespace Uta.Client.Tests
{
    public sealed class PlaceholderCharacterStageTests
    {
        private GameObject go;
        private TextMeshProUGUI label;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject();
            label = go.AddComponent<TextMeshProUGUI>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ShowIdol_SetsIdolName()
        {
            var stage = new PlaceholderCharacterStage(label);

            stage.ShowIdol(2, "テストアイドル");

            Assert.AreEqual("テストアイドル", label.text);
        }

        [Test]
        public void Clear_SetsEmptyText()
        {
            var stage = new PlaceholderCharacterStage(label);
            stage.ShowIdol(2, "テストアイドル");

            stage.Clear();

            Assert.AreEqual(PlaceholderCharacterStage.EmptyText, label.text);
            Assert.AreEqual("キャラ表示", PlaceholderCharacterStage.EmptyText);
        }

        [Test]
        public void Constructor_Null_Throws()
        {
            var e = Assert.Throws<ArgumentNullException>(() => new PlaceholderCharacterStage(null));

            Assert.AreEqual("label", e.ParamName);
        }
    }
}

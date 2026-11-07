using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator;
using UnityScreenNavigator.Runtime.Core.Modal;
using Uta.Client.Editor.Prefabs;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Infrastructure.Tests
{
    /// <summary>Assets/Prefabs の下の Prefab の契約（参照の設定・部品・UiRoot の層・Addressables・TMP Settings）。</summary>
    public sealed class PrefabContractTests
    {
        private const string PrefabsFolder = "Assets/Prefabs";

        private static readonly string[] PartNames =
        {
            "Label", "Button", "Panel", "ListRow", "Header", "Tab", "TabBar", "Dialog", "BusyBlocker", "UiRoot",
        };

        private static readonly string[] UiRootLayers =
        {
            "Background", "Pages", "Modals", "Overlays", "Busy", "Dialogs", "EventSystem",
        };

        [Test]
        public void AllPrefabs_SerializedReferences_AreAssigned()
        {
            var errors = new List<string>();
            foreach (var path in PrefabContract.FindPrefabPaths(PrefabsFolder))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var missing in PrefabContract.CollectMissingReferences(prefab))
                {
                    errors.Add(path + ": " + missing);
                }
            }

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Parts_AllExist()
        {
            foreach (var name in PartNames)
            {
                var path = PrefabBuildUtil.PartPath(name);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
            }
        }

        [Test]
        public void UiRoot_HasLayersInOrder()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBuildUtil.PartPath("UiRoot"));
            Assert.IsNotNull(root);

            var names = new List<string>();
            foreach (Transform child in root.transform)
            {
                names.Add(child.name);
            }

            Assert.That(names, Is.EqualTo(UiRootLayers));

            var overlays = root.transform.Find("Overlays");
            Assert.IsNotNull(overlays.GetComponent<ModalContainer>());
            Assert.IsNotNull(overlays.GetComponent<OverlayContainer>());

            var scaler = root.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution);
            Assert.AreEqual(0f, scaler.matchWidthOrHeight);
        }

        [Test]
        public void ScreenPrefabs_RootViewIsNamedAfterPrefab()
        {
            foreach (var path in PrefabContract.FindPrefabPaths(PrefabBuildUtil.ScreensFolder))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                var page = prefab.GetComponent<PageBase>();
                var modal = prefab.GetComponent<ModalBase>();
                var view = page != null ? (Component)page : modal;
                Assert.IsNotNull(view, path + ": no PageBase/ModalBase on root");
                Assert.AreEqual(fileName, view.GetType().Name, path);
            }
        }

        [Test]
        public void ScreenPrefabs_AreRegisteredInScreensGroup()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var path in PrefabContract.FindPrefabPaths(PrefabBuildUtil.ScreensFolder))
            {
                Assert.IsNotNull(settings, "Addressables settings not found");
                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.IsNotNull(entry, path + ": not registered");
                Assert.AreEqual(AddressablesSetup.ScreensGroupName, entry.parentGroup.Name, path);
                Assert.AreEqual(Path.GetFileNameWithoutExtension(path), entry.address, path);
                Assert.That(entry.labels, Is.Empty, path);
            }
        }

        [Test]
        public void ScreensGroup_ContainsOnlyScreenPrefabs()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "Addressables settings not found");
            var group = settings.FindGroup(AddressablesSetup.ScreensGroupName);
            Assert.IsNotNull(group, "group not found: " + AddressablesSetup.ScreensGroupName);

            var prefix = PrefabBuildUtil.ScreensFolder + "/";
            foreach (var entry in group.entries)
            {
                StringAssert.StartsWith(prefix, entry.AssetPath);
            }
        }

        [Test]
        public void TmpSettings_DefaultFont_IsNotoSansJpDynamic()
        {
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssets.RegularPath);
            Assert.IsNotNull(regular, FontAssets.RegularPath);
            Assert.AreEqual(regular, TMP_Settings.defaultFontAsset);
            Assert.AreEqual(AtlasPopulationMode.Dynamic, regular.atlasPopulationMode);

            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssets.BoldPath);
            Assert.IsNotNull(bold, FontAssets.BoldPath);
            Assert.AreEqual(AtlasPopulationMode.Dynamic, bold.atlasPopulationMode);
        }
    }
}

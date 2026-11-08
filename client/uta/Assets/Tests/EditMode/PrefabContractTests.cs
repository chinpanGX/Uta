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
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Tests
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
            "Background", "Pages", "Modals", "Busy", "Overlays", "EventSystem",
        };

        // 根 Broken と子 Child。Child に存在しない GUID のスクリプトの MonoBehaviour を付ける
        private const string BrokenPrefabYaml =
            "%YAML 1.1\n" +
            "%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!1 &100\n" +
            "GameObject:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  serializedVersion: 6\n" +
            "  m_Component:\n" +
            "  - component: {fileID: 101}\n" +
            "  m_Layer: 0\n" +
            "  m_Name: Broken\n" +
            "  m_TagString: Untagged\n" +
            "  m_IsActive: 1\n" +
            "--- !u!4 &101\n" +
            "Transform:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_GameObject: {fileID: 100}\n" +
            "  serializedVersion: 2\n" +
            "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n" +
            "  m_LocalPosition: {x: 0, y: 0, z: 0}\n" +
            "  m_LocalScale: {x: 1, y: 1, z: 1}\n" +
            "  m_Children:\n" +
            "  - {fileID: 201}\n" +
            "  m_Father: {fileID: 0}\n" +
            "--- !u!1 &200\n" +
            "GameObject:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  serializedVersion: 6\n" +
            "  m_Component:\n" +
            "  - component: {fileID: 201}\n" +
            "  - component: {fileID: 202}\n" +
            "  m_Layer: 0\n" +
            "  m_Name: Child\n" +
            "  m_TagString: Untagged\n" +
            "  m_IsActive: 1\n" +
            "--- !u!4 &201\n" +
            "Transform:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_GameObject: {fileID: 200}\n" +
            "  serializedVersion: 2\n" +
            "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n" +
            "  m_LocalPosition: {x: 0, y: 0, z: 0}\n" +
            "  m_LocalScale: {x: 1, y: 1, z: 1}\n" +
            "  m_Children: []\n" +
            "  m_Father: {fileID: 101}\n" +
            "--- !u!114 &202\n" +
            "MonoBehaviour:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_GameObject: {fileID: 200}\n" +
            "  m_Enabled: 1\n" +
            "  m_EditorHideFlags: 0\n" +
            "  m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}\n" +
            "  m_Name: \n" +
            "  m_EditorClassIdentifier: \n";

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
        public void CollectMissingReferences_MissingScript_IsReported()
        {
            // 存在しないスクリプトの GUID を参照する Prefab を一時フォルダに書き、読み込んで確かめる（終わったらフォルダごと消す）
            var folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder("Assets", "UtaTempMissingScriptTest"));
            try
            {
                var path = folder + "/Broken.prefab";
                File.WriteAllText(path, BrokenPrefabYaml);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);

                var missing = PrefabContract.CollectMissingReferences(prefab);

                Assert.That(missing, Is.EqualTo(new[] { "Broken/Child.<missing script>" }));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
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

            var uiRoot = new SerializedObject(root.GetComponent<UiRoot>());
            Assert.IsNull(uiRoot.FindProperty("dialogLayer"));
            Assert.IsNull(uiRoot.FindProperty("dialogPrefab"));

            var scaler = root.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution);
            Assert.AreEqual(0f, scaler.matchWidthOrHeight);
        }

        [Test]
        public void Dialog_RootIsModalWithPanel_AndRegisteredInScreensGroup()
        {
            var dialog = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBuildUtil.PartPath("Dialog"));
            var view = dialog.GetComponent<DialogView>();
            Assert.IsNotNull(view);
            Assert.IsNotNull(view.GetComponent<Modal>());
            Assert.IsNotNull(new SerializedObject(view).FindProperty("panel").objectReferenceValue);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "Addressables settings not found");
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(PrefabBuildUtil.PartPath("Dialog")));
            Assert.IsNotNull(entry);
            Assert.AreEqual(AddressablesSetup.ScreensGroupName, entry.parentGroup.Name);
            Assert.AreEqual("Dialog", entry.address);
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
        public void ScreensGroup_ContainsOnlyScreenPrefabsAndDialog()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "Addressables settings not found");
            var group = settings.FindGroup(AddressablesSetup.ScreensGroupName);
            Assert.IsNotNull(group, "group not found: " + AddressablesSetup.ScreensGroupName);

            var prefix = PrefabBuildUtil.ScreensFolder + "/";
            foreach (var entry in group.entries)
            {
                if (entry.AssetPath == PrefabBuildUtil.PartPath("Dialog"))
                {
                    continue;   // Dialog は USN の Overlay に出すので Screens グループに入れる
                }

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

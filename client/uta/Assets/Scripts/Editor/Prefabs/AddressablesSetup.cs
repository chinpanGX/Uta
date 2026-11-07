using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Uta.Client.Editor.Prefabs
{
    public static class AddressablesSetup
    {
        public const string ScreensGroupName = "Screens";

        public static AddressableAssetGroup EnsureSettingsAndGroup()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            var group = settings.FindGroup(ScreensGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(ScreensGroupName, false, false, true, settings.DefaultGroup.Schemas);
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return group;
        }

        public static void RegisterScreen(string prefabPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
            {
                throw new System.InvalidOperationException("asset not found: " + prefabPath);
            }
            var group = EnsureSettingsAndGroup();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.CreateOrMoveEntry(guid, group, false, true);
            entry.address = Path.GetFileNameWithoutExtension(prefabPath);
            foreach (var label in entry.labels.ToList())
            {
                entry.SetLabel(label, false);
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}

using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>プロジェクトの TMP Settings を作り、既定のフォントを設定する（Unity 7 の uGUI 7.0.0 には Essential Resources の取り込みが無い）。</summary>
    public static class TmpSettingsBuilder
    {
        public const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string BuiltInSettingsGuid = "0104df30113748678d4d2f9079d0d107";

        public static void EnsureProjectSettings(TMP_FontAsset font)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
            if (settings == null)
            {
                var builtInPath = AssetDatabase.GUIDToAssetPath(BuiltInSettingsGuid);
                var builtIn = AssetDatabase.LoadAssetAtPath<TMP_Settings>(builtInPath);
                if (builtIn == null)
                {
                    throw new InvalidOperationException("built-in TMP Settings not found: " + BuiltInSettingsGuid);
                }
                settings = UnityEngine.Object.Instantiate(builtIn);
                settings.name = "TMP Settings";
                var created = new SerializedObject(settings);
                created.FindProperty("m_IsBuiltInSettings").boolValue = false;
                created.ApplyModifiedPropertiesWithoutUndo();
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SettingsPath);

            if (TMP_Settings.defaultFontAsset != font)
            {
                throw new InvalidOperationException("TMP_Settings.defaultFontAsset was not set");
            }

            foreach (var guid in AssetDatabase.FindAssets("t:TMP_Settings"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != SettingsPath && !path.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("another TMP_Settings exists: " + path);
                }
            }
        }
    }
}

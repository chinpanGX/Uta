using System;
using TMPro;
using UnityEditor;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>作成済みの Noto Sans JP の動的 SDF フォントアセットの読み込みと確かめ。アセットは作らず、設定も変えない。</summary>
    public static class FontAssets
    {
        public const string RegularPath = "Assets/Fonts/NotoSansJP/NotoSansJP-Regular SDF.asset";
        public const string BoldPath = "Assets/Fonts/NotoSansJP/NotoSansJP-Bold SDF.asset";

        public static TMP_FontAsset LoadRegular()
        {
            return Load(RegularPath);
        }

        public static TMP_FontAsset LoadBold()
        {
            return Load(BoldPath);
        }

        private static TMP_FontAsset Load(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                throw new InvalidOperationException("font asset not found: " + path + "（PR #29 のフォントが main に入っているか確かめる）");
            }
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                throw new InvalidOperationException("font asset is not dynamic: " + path);
            }
            return font;
        }

        /// <summary>Editor で文字を描いて足された動的データを空に戻す（コミット済みのアセットはアトラスが空）。</summary>
        public static void ClearDynamicData()
        {
            foreach (var path in new[] { RegularPath, BoldPath })
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null)
                {
                    continue;
                }
                font.ClearFontAssetData(true);
                EditorUtility.SetDirty(font);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>Prefab の生成の入口。メニュー・eval・バッチモードから呼ぶ。</summary>
    public static class PrefabBuildCommands
    {
        /// <summary>
        /// フォント・TMP Settings・Addressables の設定を整え、IPrefabBuilder を Order 順に全部実行する。
        /// "built N" を返す。例外はそのまま投げる。
        /// </summary>
        public static string BuildAll()
        {
            var regular = FontAssets.LoadRegular();
            FontAssets.LoadBold();
            TmpSettingsBuilder.EnsureProjectSettings(regular);
            AddressablesSetup.EnsureSettingsAndGroup();

            var builders = CollectBuilders();

            var session = new PrefabBuildSession();
            try
            {
                foreach (var builder in builders)
                {
                    builder.Build(session);
                }
            }
            finally
            {
                // 後片付けの失敗はログに出すだけにし、Build の元の例外を隠さない
                RunCleanup(session.Dispose);
                RunCleanup(FontAssets.ClearDynamicData);
                RunCleanup(AssetDatabase.SaveAssets);
            }

            var result = "built " + builders.Count;
            Debug.Log("[Uta] prefabs: " + result);
            return result;
        }

        private static List<IPrefabBuilder> CollectBuilders()
        {
            var builders = new List<IPrefabBuilder>();
            var editorAssembly = typeof(IPrefabBuilder).Assembly;
            foreach (var type in TypeCache.GetTypesDerivedFrom<IPrefabBuilder>())
            {
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                {
                    continue;
                }

                // Uta.Client.Editor の型だけ（テストなどほかのアセンブリの実装は拾わない）
                if (type.Assembly != editorAssembly)
                {
                    continue;
                }

                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    throw new InvalidOperationException("prefab builder has no public parameterless constructor: " + type.FullName);
                }
                builders.Add((IPrefabBuilder)constructor.Invoke(null));
            }

            var duplicate = builders.GroupBy(builder => builder.AssetPath, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException("duplicate prefab asset path: " + duplicate.Key);
            }

            return builders
                .OrderBy(builder => builder.Order)
                .ThenBy(builder => builder.AssetPath, StringComparer.Ordinal)
                .ToList();
        }

        private static void RunCleanup(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>-batchmode -executeMethod 用。成功で Exit(0)、例外は Exit(1)。</summary>
        public static void BuildAllBatch()
        {
            try
            {
                BuildAll();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Uta/Prefabs/Build All")]
        private static void BuildAllMenu()
        {
            BuildAll();
        }
    }
}

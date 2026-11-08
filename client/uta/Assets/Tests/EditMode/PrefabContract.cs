using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Tests
{
    /// <summary>Prefab の契約の確かめの補助。P2・P3 のテストも使う。</summary>
    public static class PrefabContract
    {
        private const string PresentationAssemblyName = "Uta.Client.Presentation";

        /// <summary>folder の下の全 Prefab のパス（序数順）。フォルダが無ければ空。</summary>
        public static string[] FindPrefabPaths(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new string[0];
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            var paths = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(StringComparer.Ordinal);
            return paths.ToArray();
        }

        /// <summary>
        /// prefab の子孫（非アクティブも含む）の Uta.Client.Presentation の MonoBehaviour について、
        /// 未設定（null）の ObjectReference を集める。USN の基底のフィールドと [OptionalReference] の付いたものは除く。
        /// 戻り値は "&lt;GameObject の階層のパス&gt;.&lt;型名&gt;.&lt;プロパティのパス&gt;" の一覧。
        /// スクリプトが外れたコンポーネント（Missing Script）がある GameObject は "&lt;GameObject の階層のパス&gt;.&lt;missing script&gt;" を足す。
        /// プロパティは HideInInspector のものも見る（Next）。Unity の側の項目（m_Script・m_GameObject など、C# のフィールドが無いもの）は除く。
        /// </summary>
        public static List<string> CollectMissingReferences(GameObject prefab)
        {
            var missing = new List<string>();
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                {
                    missing.Add(GetHierarchyPath(transform) + ".<missing script>");
                }
            }

            foreach (var behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                var type = behaviour.GetType();
                if (type.Assembly.GetName().Name != PresentationAssemblyName)
                {
                    continue;
                }

                var serialized = new SerializedObject(behaviour);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (property.objectReferenceValue != null)
                    {
                        continue;
                    }

                    var propertyPath = property.propertyPath;
                    if (propertyPath == "m_Script")
                    {
                        continue;
                    }

                    var dot = propertyPath.IndexOf('.');
                    var fieldName = dot < 0 ? propertyPath : propertyPath.Substring(0, dot);
                    var field = FindField(type, fieldName);
                    if (field == null)
                    {
                        // Unity の側の項目（m_CorrespondingSourceObject・m_PrefabInstance など）。Next で見えるようになる
                        continue;
                    }

                    if (field.DeclaringType.Assembly.GetName().Name != PresentationAssemblyName)
                    {
                        continue;
                    }

                    if (field.IsDefined(typeof(OptionalReferenceAttribute), false))
                    {
                        continue;
                    }

                    missing.Add(GetHierarchyPath(behaviour.transform) + "." + type.Name + "." + propertyPath);
                }
            }

            return missing;
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Uta.Client.Tests
{
    /// <summary>画面の Prefab の確かめの補助（P3 も使う）。</summary>
    public static class ScreenPrefabAssert
    {
        /// <summary>AssetDatabase.LoadAssetAtPath&lt;GameObject&gt;(assetPath)。null なら Assert.Fail("prefab not found: " + assetPath)。</summary>
        public static GameObject Load(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Assert.Fail("prefab not found: " + assetPath);
            }
            return prefab;
        }

        /// <summary>prefab のルートの GetComponent&lt;T&gt;()。null なら Assert.Fail(assetPath + ": root has no " + typeof(T).Name)。</summary>
        public static T RootView<T>(GameObject prefab) where T : Component
        {
            var view = prefab.GetComponent<T>();
            if (view == null)
            {
                Assert.Fail(AssetDatabase.GetAssetPath(prefab) + ": root has no " + typeof(T).Name);
            }
            return view;
        }

        /// <summary>
        /// view のフィールド fieldName の参照（SerializedObject の objectReferenceValue）が、
        /// view.transform.Find(childPath) の GameObject 上のコンポーネント（GameObject そのものでもよい）であること。
        /// 失敗の文に view の型名・fieldName・childPath を入れる。
        /// </summary>
        public static void FieldIsChild(Component view, string fieldName, string childPath)
        {
            var label = view.GetType().Name + "." + fieldName + " -> " + childPath;
            var value = ReferenceOf(view, fieldName);
            var child = view.transform.Find(childPath);
            if (child == null)
            {
                Assert.Fail(label + ": " + childPath + " not found");
            }

            var component = value as Component;
            if (component != null)
            {
                Assert.AreEqual(child.gameObject, component.gameObject, label);
                return;
            }

            var gameObject = value as GameObject;
            if (gameObject != null)
            {
                Assert.AreEqual(child.gameObject, gameObject, label);
                return;
            }

            Assert.Fail(label + ": reference is null or not a Component/GameObject");
        }

        /// <summary>同じく、参照の AssetDatabase.GetAssetPath が assetPath であること（Prefab のアセットの参照）。</summary>
        public static void FieldIsAsset(Component view, string fieldName, string assetPath)
        {
            var label = view.GetType().Name + "." + fieldName + " -> " + assetPath;
            var value = ReferenceOf(view, fieldName);
            if (value == null)
            {
                Assert.Fail(label + ": reference is null");
            }
            Assert.AreEqual(assetPath, AssetDatabase.GetAssetPath(value), label);
        }

        private static Object ReferenceOf(Component view, string fieldName)
        {
            var property = new SerializedObject(view).FindProperty(fieldName);
            if (property == null)
            {
                Assert.Fail(view.GetType().Name + "." + fieldName + ": field not found");
            }
            return property.objectReferenceValue;
        }
    }
}

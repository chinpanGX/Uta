using System;
using UnityEngine;

namespace Uta.Client.Presentation.Common
{
    public static class UiObjects
    {
        /// <summary>target が null なら何もしない。Application.isPlaying なら target.SetActive(false) してから Object.Destroy（レイアウトから即座に外すため）、そうでなければ Object.DestroyImmediate。</summary>
        public static void Destroy(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                target.SetActive(false);
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        /// <summary>parent の子を最後から順に全部 Destroy する。</summary>
        public static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 一覧の行を作る: Object.Instantiate(rowPrefab, parent, false)。名前は name。戻り値はその行の View。
        /// rowPrefab・parent が null なら ArgumentNullException。
        /// </summary>
        public static T AddRow<T>(T rowPrefab, Transform parent, string name) where T : Component
        {
            if (rowPrefab == null)
            {
                throw new ArgumentNullException(nameof(rowPrefab));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var row = UnityEngine.Object.Instantiate(rowPrefab, parent, false);
            row.gameObject.name = name;
            return row;
        }
    }
}

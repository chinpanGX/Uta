using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>編成・プロデュースの画面だけが使う組み方の補助（P3 の計画 3.3）。ScreenLayout に無いものだけを置く。</summary>
    public static class ProduceLayout
    {
        /// <summary>
        /// PrefabBuildUtil.AddHeader(body, title) のあと、ネストした BackButton を非アクティブにし、TitleText を左寄せ（56/Text/MidlineLeft/NoWrap/Ellipsis）にする。
        /// 子が見つからなければ InvalidOperationException("Header child not found: " + 名前)。
        /// </summary>
        public static HeaderView AddHeaderWithoutBack(RectTransform body, string title)
        {
            var header = PrefabBuildUtil.AddHeader(body, title);

            var backButton = FindHeaderChild(header, "BackButton");
            backButton.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(backButton.gameObject);

            var titleText = FindHeaderChild(header, "TitleText").GetComponent<TMP_Text>();
            if (titleText == null)
            {
                throw new InvalidOperationException("Header child not found: TitleText");
            }
            PrefabBuildUtil.ApplyText(titleText, new TextSpec(UiTheme.FontTitle, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true));
            return header;
        }

        /// <summary>
        /// header の RightSlot に AddButtonPart(slot, "MenuButton", "≡", ButtonStyle.Secondary) を置き、SetRect(Stretch())。
        /// （RightSlot はレイアウトグループを持たないので、全面で 200×130 を埋める）
        /// </summary>
        public static ButtonView AddMenuButton(HeaderView header)
        {
            var slot = FindHeaderChild(header, "RightSlot");
            var button = PrefabBuildUtil.AddButtonPart(slot, "MenuButton", "≡", ButtonStyle.Secondary);
            PrefabBuildUtil.SetRect((RectTransform)button.transform, RectSpec.Stretch());
            return button;
        }

        /// <summary>AddChild(parent, name) + AddVertical(new LayoutSpec(spacing, 0, 0, 0, 0, UpperCenter, true, true, true, false))。子は無し（実行時に並べる）。</summary>
        public static RectTransform AddRuntimeList(RectTransform parent, string name, float spacing)
        {
            var list = PrefabBuildUtil.AddChild(parent, name);
            PrefabBuildUtil.AddVertical(list.gameObject, new LayoutSpec(spacing, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false));
            return list;
        }

        private static Transform FindHeaderChild(HeaderView header, string childName)
        {
            var child = header.transform.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException("Header child not found: " + childName);
            }
            return child;
        }
    }
}

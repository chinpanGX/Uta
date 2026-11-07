using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>画面の生成クラスが共通で使う形と文字の型。P3 も使う（P3 は同じ役割の関数を作らない）。</summary>
    public static class ScreenLayout
    {
        /// <summary>Body の VLG: spacing 0、padding 0、UpperCenter、制御 true/true、伸ばす true/false。</summary>
        public static LayoutSpec BodyLayout => new LayoutSpec(0f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false);

        // 文字の型（TextSpec(fontSize, color, alignment, wrap, ellipsis)）
        public static TextSpec Title => TextSpec.Title;                                                                    // 56, Text, Center, false, true
        public static TextSpec Line => TextSpec.SingleLine;                                                                // 40, Text, MidlineLeft, false, true
        public static TextSpec LineRight => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineRight, false, true);
        public static TextSpec LineCenter => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, false, true);
        public static TextSpec Block => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.TopLeft, true, false);
        public static TextSpec BlockCenter => new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.Center, true, true);
        public static TextSpec Heading => new TextSpec(UiTheme.FontBody, UiTheme.Accent, TextAlignmentOptions.MidlineLeft, false, true);
        public static TextSpec SmallLine => new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false, true);
        public static TextSpec SmallBlock => new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.TopLeft, true, true);
        public static TextSpec SmallButton => new TextSpec(UiTheme.FontSmall, UiTheme.Text, TextAlignmentOptions.Center, false, true);

        /// <summary>PrefabBuildUtil.AddVertical(body.gameObject, BodyLayout)。</summary>
        public static void SetupBody(RectTransform body)
        {
            PrefabBuildUtil.AddVertical(body.gameObject, BodyLayout);
        }

        /// <summary>AddChild(parent, "Main") → AddVertical(LayoutSpec.Column) → SetLayout(flexibleHeight: 1)。戻り値は Main。</summary>
        public static RectTransform AddMain(Transform parent)
        {
            var main = PrefabBuildUtil.AddChild(parent, "Main");
            PrefabBuildUtil.AddVertical(main.gameObject, LayoutSpec.Column);
            PrefabBuildUtil.SetLayout(main.gameObject, flexibleHeight: 1f);
            return main;
        }

        /// <summary>
        /// AddChild(parent, name) → AddHorizontal(LayoutSpec.Row) → SetLayout(minHeight: height, preferredHeight: height, flexibleHeight: 0)。
        /// flexibleHeight 0 を明示する: LayoutSpec.Row は伸ばす H が true なので、HLG が flexibleHeight 1 を返し、親の VLG の余りを Stage などと分け合ってしまうため。
        /// </summary>
        public static RectTransform AddRow(Transform parent, string name, float height)
        {
            var row = PrefabBuildUtil.AddChild(parent, name);
            PrefabBuildUtil.AddHorizontal(row.gameObject, LayoutSpec.Row);
            PrefabBuildUtil.SetLayout(row.gameObject, minHeight: height, preferredHeight: height, flexibleHeight: 0f);
            return row;
        }

        /// <summary>AddChild(parent, name) → SetLayout(flexibleHeight: 1)。</summary>
        public static RectTransform AddFlexibleSpacer(Transform parent, string name)
        {
            var spacer = PrefabBuildUtil.AddChild(parent, name);
            PrefabBuildUtil.SetLayout(spacer.gameObject, flexibleHeight: 1f);
            return spacer;
        }

        /// <summary>AddChild(parent, name) → SetLayout(minHeight: height, preferredHeight: height)。</summary>
        public static RectTransform AddFixedSpacer(Transform parent, string name, float height)
        {
            var spacer = PrefabBuildUtil.AddChild(parent, name);
            PrefabBuildUtil.SetLayout(spacer.gameObject, minHeight: height, preferredHeight: height);
            return spacer;
        }

        /// <summary>PrefabBuildUtil.AddLabel(parent, name, text, spec) → height > 0 なら SetLayout(minHeight: height, preferredHeight: height)。戻り値は TMP。</summary>
        public static TextMeshProUGUI AddLabel(Transform parent, string name, string text, TextSpec spec, float height)
        {
            var label = PrefabBuildUtil.AddLabel(parent, name, text, spec);
            if (height > 0f)
            {
                PrefabBuildUtil.SetLayout(label.gameObject, minHeight: height, preferredHeight: height);
            }
            return label;
        }

        /// <summary>button の子 "Label" の TMP（無ければ InvalidOperationException("Label not found: " + button.name)）に ApplyText(spec) し、text を入れる。MarkModified。</summary>
        public static void SetButtonLabel(ButtonView button, string text, TextSpec spec)
        {
            var child = button.transform.Find("Label");
            var label = child != null ? child.GetComponent<TMP_Text>() : null;
            if (label == null)
            {
                throw new InvalidOperationException("Label not found: " + button.name);
            }
            PrefabBuildUtil.ApplyText(label, spec);
            label.text = text;
            PrefabBuildUtil.MarkModified(label);
        }

        /// <summary>AssetDatabase.LoadAssetAtPath&lt;GameObject&gt;(PrefabBuildUtil.PartPath(partName)) のルートの GetComponent&lt;T&gt;()。Prefab か T が無ければ InvalidOperationException("part not found: " + partName)。</summary>
        public static T LoadPart<T>(string partName) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBuildUtil.PartPath(partName));
            var component = prefab != null ? prefab.GetComponent<T>() : null;
            if (component == null)
            {
                throw new InvalidOperationException("part not found: " + partName);
            }
            return component;
        }
    }
}

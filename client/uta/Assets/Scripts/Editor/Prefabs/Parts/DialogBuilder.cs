using System;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>Dialog（確認・お知らせ・選択。USN の Overlay に出る。ボタンは DialogPresenter が実行時に並べる）を作る。</summary>
    public sealed class DialogBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 170; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("Dialog"); }
        }

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot("Dialog");
            // 下の入力を止める
            PrefabBuildUtil.AddImage(root, UiTheme.Overlay, true);
            var view = root.AddComponent<DialogView>();

            // 高さは ContentSizeFitter が決める
            var panel = PrefabBuildUtil.AddChild(root.transform, "Panel", RectSpec.Center(UiTheme.DialogWidth, 0f));
            PrefabBuildUtil.AddImage(panel.gameObject, UiTheme.Panel, true);
            PrefabBuildUtil.AddVertical(panel.gameObject, new LayoutSpec(32f, 40, 40, 40, 40, TextAnchor.UpperCenter, true, true, true, false));
            PrefabBuildUtil.AddVerticalFitter(panel.gameObject);

            var messageText = PrefabBuildUtil.AddLabel(panel, "MessageText", "メッセージ", TextSpec.BodyCenter);

            var buttonList = PrefabBuildUtil.AddChild(panel, "ButtonList");
            PrefabBuildUtil.AddVertical(buttonList.gameObject, new LayoutSpec(UiTheme.Spacing, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false));

            var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBuildUtil.PartPath("Button"));
            if (buttonPrefab == null)
            {
                throw new InvalidOperationException("part not found: Button");
            }

            PrefabBuildUtil.SetRef(view, "panel", panel);
            PrefabBuildUtil.SetRef(view, "messageText", messageText);
            PrefabBuildUtil.SetRef(view, "buttonList", buttonList);
            PrefabBuildUtil.SetRef(view, "buttonPrefab", buttonPrefab.GetComponent<ButtonView>());
            PrefabBuildUtil.SavePrefab(root, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}

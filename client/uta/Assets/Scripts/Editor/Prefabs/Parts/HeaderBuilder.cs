using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>共通部品 Header（戻るボタン・タイトル・右の置き場）を作る。</summary>
    public sealed class HeaderBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 160; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("Header"); }
        }

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot("Header");
            PrefabBuildUtil.SetRect((RectTransform)root.transform, RectSpec.Top(UiTheme.HeaderHeight));
            PrefabBuildUtil.AddImage(root, UiTheme.Header, true);
            PrefabBuildUtil.AddHorizontal(root, new LayoutSpec(16f, 24, 24, 10, 10, TextAnchor.MiddleCenter, true, true, false, true));
            // flexibleHeight 0: HLG の伸ばす H が true なので、明示しないと flexibleHeight 1 になり、親の VLG の余りを吸って高くなる
            PrefabBuildUtil.SetLayout(root, minHeight: UiTheme.HeaderHeight, preferredHeight: UiTheme.HeaderHeight, flexibleHeight: 0f);
            var view = root.AddComponent<HeaderView>();

            var backButton = PrefabBuildUtil.AddButtonPart(root.transform, "BackButton", "戻る", ButtonStyle.Secondary);
            PrefabBuildUtil.SetLayout(backButton.gameObject, preferredWidth: 200f, flexibleWidth: 0f);

            var titleText = PrefabBuildUtil.AddLabel(root.transform, "TitleText", "タイトル", TextSpec.Title);
            PrefabBuildUtil.SetLayout(titleText.gameObject, flexibleWidth: 1f);

            // タイトルを中央に保つ。画面がここにボタンを全面で置く
            var rightSlot = PrefabBuildUtil.AddChild(root.transform, "RightSlot");
            PrefabBuildUtil.SetLayout(rightSlot.gameObject, preferredWidth: 200f, flexibleWidth: 0f);

            PrefabBuildUtil.SetRef(view, "backButton", backButton);
            PrefabBuildUtil.SetRef(view, "titleText", titleText);
            PrefabBuildUtil.SetRef(view, "rightSlot", rightSlot);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}

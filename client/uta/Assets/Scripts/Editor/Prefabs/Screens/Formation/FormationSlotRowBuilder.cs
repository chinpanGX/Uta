using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Formation;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>編成のサポートカードの枠の行（Parts/FormationSlotRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class FormationSlotRowBuilder : IPrefabBuilder
    {
        private const string Name = "FormationSlotRow";

        public int Order => 1000;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot(Name);
            PrefabBuildUtil.AddImage(root, UiTheme.Panel, false);
            // flexibleHeight 0: HLG の伸ばす H が true なので、明示しないと flexibleHeight 1 になり、親の VLG の余りを吸う
            PrefabBuildUtil.SetLayout(root, minHeight: 130f, preferredHeight: 130f, flexibleHeight: 0f);
            PrefabBuildUtil.AddHorizontal(root, new LayoutSpec(16f, 24, 24, 10, 10, TextAnchor.MiddleLeft, true, true, false, true));
            var view = root.AddComponent<FormationSlotRowView>();

            var labelText = PrefabBuildUtil.AddLabel(root.transform, "LabelText", "枠: （空）",
                ScreenLayout.SmallLine);
            PrefabBuildUtil.SetLayout(labelText.gameObject, preferredWidth: 0f, flexibleWidth: 1f);
            var select = PrefabBuildUtil.AddButtonPart(root.transform, "SelectButton", "選ぶ", ButtonStyle.Primary);
            PrefabBuildUtil.SetLayout(select.gameObject, minHeight: 110f, preferredHeight: 110f, preferredWidth: 180f, flexibleWidth: 0f);
            var clear = PrefabBuildUtil.AddButtonPart(root.transform, "ClearButton", "外す", ButtonStyle.Secondary);
            PrefabBuildUtil.SetLayout(clear.gameObject, minHeight: 110f, preferredHeight: 110f, preferredWidth: 180f, flexibleWidth: 0f);

            PrefabBuildUtil.SetRef(view, "labelText", labelText);
            PrefabBuildUtil.SetRef(view, "selectButton", select);
            PrefabBuildUtil.SetRef(view, "clearButton", clear);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}

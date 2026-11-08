using System;
using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Formation;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>編成のページ（Screens/Formation/FormationPage.prefab）を作る。Addressables の Screens に登録する。</summary>
    public sealed class FormationPageBuilder : IPrefabBuilder
    {
        // FormationPresenter.SupportSlots と同じ（Editor のアセンブリは Presenter を参照しないので定数を写す）
        private const int FormationSlotCount = 5;

        public int Order => 2000;

        public string AssetPath => PrefabBuildUtil.ScreenPath("Formation", "FormationPage");

        public void Build()
        {
            var page = PrefabBuildUtil.CreatePage<FormationPage>("FormationPage", out var body);
            ScreenLayout.SetupBody(body);
            var header = PrefabBuildUtil.AddHeader(body, "編成");
            var main = ScreenLayout.AddMain(body);

            var idolPanel = PrefabBuildUtil.AddPanel(main, "IdolPanel");
            var idolText = ScreenLayout.AddLabel(idolPanel, "IdolText", "育成アイドル: 未選択",
                new TextSpec(UiTheme.FontBody, UiTheme.Text, TextAlignmentOptions.MidlineLeft, true, true), 110f);
            var selectIdol = PrefabBuildUtil.AddButtonPart(idolPanel, "SelectIdolButton", "育成アイドルを選ぶ", ButtonStyle.Primary);

            ScreenLayout.AddLabel(main, "SlotsTitle", "サポートカード", ScreenLayout.Line, 60f);
            var slotList = ProduceLayout.AddRuntimeList(main, "SlotList", 16f);
            var slotViews = new FormationSlotRowView[FormationSlotCount];
            for (var i = 0; i < FormationSlotCount; i++)
            {
                slotViews[i] = PrefabBuildUtil.InstantiatePart<FormationSlotRowView>(slotList, "FormationSlotRow", "Slot" + i);
                // 枠ごとのラベルの初期値（ネストした FormationSlotRow の中の LabelText の TMP の text をオーバーライドする）
                var labelTransform = slotViews[i].transform.Find("LabelText");
                if (labelTransform == null)
                {
                    throw new InvalidOperationException("FormationSlotRow child not found: LabelText");
                }
                var label = labelTransform.GetComponent<TextMeshProUGUI>();
                label.text = "枠" + (i + 1) + ": （空）";
                PrefabBuildUtil.MarkModified(label);
            }

            var scenarioRow = PrefabBuildUtil.AddChild(main, "ScenarioRow");
            PrefabBuildUtil.AddHorizontal(scenarioRow.gameObject, new LayoutSpec(16f, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true, false, true));
            // flexibleHeight 0: HLG の伸ばす H が true なので、明示しないと flexibleHeight 1 になり、親の VLG の余りを吸う
            PrefabBuildUtil.SetLayout(scenarioRow.gameObject, minHeight: 130f, preferredHeight: 130f, flexibleHeight: 0f);
            var scenarioText = PrefabBuildUtil.AddLabel(scenarioRow, "ScenarioText", "シナリオ: なし", ScreenLayout.Line);
            PrefabBuildUtil.SetLayout(scenarioText.gameObject, flexibleWidth: 1f);
            var changeScenario = PrefabBuildUtil.AddButtonPart(scenarioRow, "ChangeScenarioButton", "変更", ButtonStyle.Secondary);
            PrefabBuildUtil.SetLayout(changeScenario.gameObject, preferredWidth: 200f, flexibleWidth: 0f);

            ScreenLayout.AddFlexibleSpacer(main, "Spacer");
            var start = PrefabBuildUtil.AddButtonPart(main, "StartButton", "プロデュース開始", ButtonStyle.Primary);

            PrefabBuildUtil.SetRef(page, "header", header);
            PrefabBuildUtil.SetRef(page, "idolText", idolText);
            PrefabBuildUtil.SetRef(page, "selectIdolButton", selectIdol);
            PrefabBuildUtil.SetRefs(page, "slots", slotViews);
            PrefabBuildUtil.SetRef(page, "scenarioText", scenarioText);
            PrefabBuildUtil.SetRef(page, "changeScenarioButton", changeScenario);
            PrefabBuildUtil.SetRef(page, "startButton", start);

            PrefabBuildUtil.SavePrefab(page.gameObject, AssetPath);
            PrefabBuildUtil.RegisterScreen(AssetPath);
        }
    }
}

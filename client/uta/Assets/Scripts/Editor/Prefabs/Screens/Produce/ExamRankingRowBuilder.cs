using TMPro;
using UnityEngine;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>試験結果の順位表の行（Parts/ExamRankingRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class ExamRankingRowBuilder : IPrefabBuilder
    {
        private const string Name = "ExamRankingRow";

        public int Order => 1040;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build(PrefabBuildSession session)
        {
            var root = session.CreateRoot(Name);
            var image = PrefabBuildUtil.AddImage(root, UiTheme.Panel, false);
            PrefabBuildUtil.SetLayout(root, minHeight: 100f, preferredHeight: 100f);
            var view = root.AddComponent<ExamRankingRowView>();

            var text = PrefabBuildUtil.AddLabel(root.transform, "Text", "1位　ライバル 1　1,000",
                ScreenLayout.Line);
            PrefabBuildUtil.SetRect((RectTransform)text.transform, RectSpec.Stretch(32f, 32f, 0f, 0f));

            PrefabBuildUtil.SetRef(view, "background", image);
            PrefabBuildUtil.SetRef(view, "text", text);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}

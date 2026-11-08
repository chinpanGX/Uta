using Uta.Client.Presentation.Gacha;

// PrefabBuildUtil・IPrefabBuilder は親の名前空間 Uta.Client.Editor.Prefabs にあるので using は要らない
namespace Uta.Client.Editor.Prefabs.Screens
{
    /// <summary>排出率の見出しの行（Parts/GachaRateHeadingRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class GachaRateHeadingRowBuilder : IPrefabBuilder
    {
        private const string Name = "GachaRateHeadingRow";

        public int Order => 1000;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build(PrefabBuildSession session)
        {
            GachaRowPrefab.Build(session, Name, "通常枠", ScreenLayout.Heading, 64f);
        }
    }

    /// <summary>排出率の行（Parts/GachaRateRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class GachaRateRowBuilder : IPrefabBuilder
    {
        private const string Name = "GachaRateRow";

        public int Order => 1000;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build(PrefabBuildSession session)
        {
            GachaRowPrefab.Build(session, Name, "SSR 育成アイドル 0.000%", ScreenLayout.SmallLine, 52f);
        }
    }

    /// <summary>ガチャの結果の 1 枠の行（Parts/GachaResultRow.prefab）を作る。Addressables に登録しない。</summary>
    public sealed class GachaResultRowBuilder : IPrefabBuilder
    {
        private const string Name = "GachaResultRow";

        public int Order => 1000;

        public string AssetPath => PrefabBuildUtil.PartPath(Name);

        public void Build(PrefabBuildSession session)
        {
            GachaRowPrefab.Build(session, Name, "1. SSR 育成アイドル 名前　NEW", ScreenLayout.SmallBlock, 96f);
        }
    }

    /// <summary>ガチャの行の Prefab の共通の作り方（ルート 1 つ: TMP・LayoutElement・GachaLineRowView）。</summary>
    internal static class GachaRowPrefab
    {
        public static void Build(PrefabBuildSession session, string name, string sampleText, TextSpec spec, float height)
        {
            var root = session.CreateRoot(name);
            var text = PrefabBuildUtil.AddText(root, sampleText, spec);
            PrefabBuildUtil.SetLayout(root, minHeight: height, preferredHeight: height);
            var row = root.AddComponent<GachaLineRowView>();
            PrefabBuildUtil.SetRef(row, "text", text);
            PrefabBuildUtil.SavePrefab(root, PrefabBuildUtil.PartPath(name));
        }
    }
}

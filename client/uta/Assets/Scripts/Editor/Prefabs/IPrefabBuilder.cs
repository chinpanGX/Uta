namespace Uta.Client.Editor.Prefabs
{
    /// <summary>Prefab を 1 つ作る生成クラス。引数なしのコンストラクターを持つ。PrefabBuildCommands.BuildAll が TypeCache で集める。</summary>
    public interface IPrefabBuilder
    {
        /// <summary>小さい順に作る。部品 100〜199、画面の行の部品 1000〜1999、画面 2000〜。同じ値なら AssetPath の序数順。</summary>
        int Order { get; }

        /// <summary>作る Prefab のパス（"Assets/Prefabs/..." で始まり ".prefab" で終わる）。</summary>
        string AssetPath { get; }

        /// <summary>Prefab を作って AssetPath に保存する（既にあれば上書き。GUID は保たれる）。</summary>
        void Build(PrefabBuildSession session);
    }
}

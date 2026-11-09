namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームのキャラ表示のステージ。MVP は PlaceholderCharacterStage、3D は MVP 外の ModelCharacterStage。ステージはマスタを読まない（ADR 0020）。</summary>
    public interface ICharacterStage
    {
        /// <summary>育成アイドル idolId を出す。3D は idolId を使い、MVP の仮の枠は name を出す。</summary>
        void ShowIdol(int idolId, string name);

        /// <summary>何も出さない（所持が 0 のとき）。</summary>
        void Clear();
    }
}

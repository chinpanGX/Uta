using Uta.Shared.Domain.Player;

namespace Uta.Client.Tests
{
    /// <summary>マスタの初期値と同じ式（Lv L の FromExp = 20 × L × (L − 1)、100 行）の PlayerLevelCatalog。Lv 12 = 2640、Lv 13 = 3120。</summary>
    public static class TestPlayerLevels
    {
        public static PlayerLevelCatalog Catalog()
        {
            var fromExps = new int[100];
            for (var i = 0; i < fromExps.Length; i++)
            {
                var level = i + 1;
                fromExps[i] = 20 * level * (level - 1);
            }

            return PlayerLevelCatalog.Create(fromExps);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Player
{
    /// <summary>プレイヤー Lv ごとの経験値の累計の下限の表（produce-server.md 4.1）。マスタ PlayerLevelExp から各側の Factory が 1 度だけ作る。</summary>
    public sealed class PlayerLevelCatalog
    {
        private readonly int[] fromExps;

        private PlayerLevelCatalog(int[] fromExps)
        {
            this.fromExps = fromExps;
        }

        /// <summary>最大 Lv（fromExps の個数）。</summary>
        public int MaxLevel => fromExps.Length;

        /// <summary>経験値の累計の上限（最後の fromExps。最大 Lv の行は FromExp = ToExp）。</summary>
        public int MaxExp => fromExps[fromExps.Length - 1];

        /// <param name="fromExps">fromExps[i] は Lv i+1 の FromExp。写して持つ。</param>
        /// <exception cref="ArgumentNullException">fromExps が null</exception>
        /// <exception cref="ArgumentException">空、fromExps[0] が 0 でない、狭義の単調増加でない</exception>
        public static PlayerLevelCatalog Create(IReadOnlyList<int> fromExps)
        {
            if (fromExps == null)
            {
                throw new ArgumentNullException(nameof(fromExps));
            }

            if (fromExps.Count == 0)
            {
                throw new ArgumentException("fromExps が空", nameof(fromExps));
            }

            if (fromExps[0] != 0)
            {
                throw new ArgumentException("Lv 1 の FromExp が 0 でない", nameof(fromExps));
            }

            for (var i = 1; i < fromExps.Count; i++)
            {
                if (fromExps[i] <= fromExps[i - 1])
                {
                    throw new ArgumentException($"Lv {i + 1} の FromExp が前の Lv 以下", nameof(fromExps));
                }
            }

            var copy = new int[fromExps.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = fromExps[i];
            }

            return new PlayerLevelCatalog(copy);
        }

        /// <summary>fromExps[i] &lt;= totalExp を満たす最大の i に 1 を足す。MaxExp を超えていても MaxLevel。</summary>
        /// <exception cref="ArgumentOutOfRangeException">totalExp が負</exception>
        public int GetLevel(int totalExp)
        {
            if (totalExp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalExp));
            }

            var r = Array.BinarySearch(fromExps, totalExp);
            return r >= 0 ? r + 1 : ~r;
        }
    }
}

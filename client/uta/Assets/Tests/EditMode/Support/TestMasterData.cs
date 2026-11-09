using System;
using MasterMemory;
using Uta.MasterData;

namespace Uta.Client.Tests
{
    /// <summary>テスト用の小さな MemoryDatabase を作る。検証（IValidatable）は走らないので、テストは要る列だけ入れる。</summary>
    public static class TestMasterData
    {
        /// <summary>append で足した行だけを持つ MemoryDatabase を作る。</summary>
        public static MemoryDatabase Build(Action<DatabaseBuilder> append)
        {
            var builder = new DatabaseBuilder();
            append(builder);
            return new MemoryDatabase(builder.Build());
        }

        /// <summary>名前とレア度だけを決めた Idol の行。Plan は Sense、MaxHp 30、Initial は 0、First/Second/ThirdParam は Singing・Rhythm・Expression。ほかの列は 0。</summary>
        public static Idol Idol(int idolId, string name, Rarity rarity)
        {
            return new Idol
            {
                IdolId = idolId,
                Name = name,
                Rarity = rarity,
                Plan = Plan.Sense,
                MaxHp = 30,
                FirstParam = ParamKind.Singing,
                SecondParam = ParamKind.Rhythm,
                ThirdParam = ParamKind.Expression,
            };
        }
    }
}

using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>テスト専用の合成カード（ID 900 以上。14章には無い）。</summary>
    public static class SyntheticCards
    {
        /// <summary>開始時手札のテスト用。消費0 で効果なし。</summary>
        public const int StartInHandDummy = 900;

        public static IReadOnlyList<CardDefinition> All()
        {
            return new[]
            {
                TestCards.Card(StartInHandDummy, Plan.Free, CardType.Mental, Rarity.N, false, false,
                    TestCards.Face(TestCards.Consume(0)), null, startInHand: true),
            };
        }
    }
}

using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using static Uta.Shared.Domain.Tests.Lesson.TestCards;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// フェーズ3のカード（lesson-card.md 14章）。ID はテスト用の値。
    /// </summary>
    public static class Phase3Cards
    {
        public const int S01 = 101;
        public const int S04 = 104;
        public const int S06 = 106;
        public const int S07 = 107;
        public const int S11 = 111;

        public static IReadOnlyList<int> AllIds => new[] { S01, S04, S06, S07, S11 };

        public static IReadOnlyList<CardDefinition> All()
        {
            return new[]
            {
                Card(S01, Plan.Sense, CardType.Mental, Rarity.N, false, false,
                    Face(Consume(1), Shield(1), Status(StatusKind.GoodCondition, 2)),
                    Face(Consume(1), Shield(1), Status(StatusKind.GoodCondition, 3))),
                Card(S04, Plan.Sense, CardType.Active, Rarity.N, true, false,
                    FaceWithCondition(Consume(2), AtLeast(ConditionSubject.GoodConditionTurns, 2), Param(10, goodScale: 200)),
                    FaceWithCondition(Consume(2), AtLeast(ConditionSubject.GoodConditionTurns, 2), Param(15, goodScale: 200))),
                Card(S06, Plan.Sense, CardType.Mental, Rarity.N, true, false,
                    Face(Consume(0), Shield(5), Status(StatusKind.Excellent, 1)),
                    Face(Consume(0), Shield(6), Status(StatusKind.Excellent, 2))),
                Card(S07, Plan.Sense, CardType.Active, Rarity.R, false, false,
                    Face(Consume(4), Param(6), Status(StatusKind.GoodCondition, 2)),
                    Face(Consume(4), Param(9), Status(StatusKind.GoodCondition, 3))),
                Card(S11, Plan.Sense, CardType.Active, Rarity.R, true, false,
                    FaceWithCondition(Consume(6), AtLeast(ConditionSubject.GoodConditionTurns, 1), Param(27)),
                    FaceWithCondition(Consume(6), AtLeast(ConditionSubject.GoodConditionTurns, 1), Param(38))),
            };
        }
    }
}

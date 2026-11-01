using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using static Uta.Shared.Domain.Tests.Lesson.TestCards;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// フェーズ4のカード（lesson-card.md 14章）。ID はテスト用の値。
    /// </summary>
    public static class Phase4Cards
    {
        public const int S02 = 102;
        public const int S03 = 103;
        public const int S05 = 105;
        public const int S08 = 108;
        public const int S09 = 109;
        public const int S10 = 110;
        public const int S12 = 112;
        public const int S13 = 113;
        public const int S14 = 114;

        public static IReadOnlyList<int> AllIds => new[] { S02, S03, S05, S08, S09, S10, S12, S13, S14 };

        public static IReadOnlyList<CardDefinition> All()
        {
            var good1 = AtLeast(ConditionSubject.GoodConditionTurns, 1);
            var focus3 = AtLeast(ConditionSubject.Focus, 3);
            return new[]
            {
                Card(S02, Plan.Sense, CardType.Mental, Rarity.N, false, false,
                    Face(Consume(1), Shield(1), Status(StatusKind.Focus, 2)),
                    Face(Consume(1), Shield(1), Status(StatusKind.Focus, 3))),
                Card(S03, Plan.Sense, CardType.Active, Rarity.N, true, false,
                    Face(Consume(3), Param(6), Status(StatusKind.Focus, 2)),
                    Face(Consume(3), Param(8), Status(StatusKind.Focus, 3))),
                Card(S05, Plan.Sense, CardType.Active, Rarity.N, true, false,
                    FaceWithCondition(Consume(2), focus3, Param(5, hits: 2)),
                    FaceWithCondition(Consume(2), focus3, Param(9, hits: 2))),
                Card(S08, Plan.Sense, CardType.Active, Rarity.R, false, false,
                    Face(Consume(4), Param(6), Status(StatusKind.Focus, 2)),
                    Face(Consume(4), Param(9), Status(StatusKind.Focus, 3))),
                Card(S09, Plan.Sense, CardType.Active, Rarity.R, false, false,
                    Face(Consume(4), Param(6), If(Status(StatusKind.Focus, 3), good1)),
                    Face(Consume(4), Param(9), If(Status(StatusKind.Focus, 4), good1))),
                Card(S10, Plan.Sense, CardType.Active, Rarity.R, true, false,
                    Face(Consume(4), Param(17, focusScale: 150)),
                    Face(Consume(4), Param(23, focusScale: 200))),
                Card(S12, Plan.Sense, CardType.Mental, Rarity.SR, true, false,
                    Face(Consume(4), Status(StatusKind.Focus, 3), Status(StatusKind.GoodCondition, 2)),
                    Face(Consume(4), Status(StatusKind.Focus, 4), Status(StatusKind.GoodCondition, 3)),
                    startInHand: true),
                Card(S13, Plan.Sense, CardType.Mental, Rarity.SR, true, true,
                    Face(Consume(4), Status(StatusKind.Focus, 2), OnActiveUse(Status(StatusKind.Focus, 1))),
                    Face(Consume(3), Status(StatusKind.Focus, 3), OnActiveUse(Status(StatusKind.Focus, 1)))),
                Card(S14, Plan.Sense, CardType.Active, Rarity.SSR, true, true,
                    Face(Consume(6), Param(15), If(Param(15), focus3)),
                    Face(Consume(6), Param(15), If(Param(34, focusScale: 150), focus3))),
            };
        }
    }
}

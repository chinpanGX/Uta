using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using static Uta.Shared.Domain.Tests.Lesson.TestCards;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// フェーズ1のカード（lesson-card.md 14章・15.2）。ID はテスト用の値。
    /// </summary>
    public static class Phase1Cards
    {
        public const int F01 = 1;
        public const int F02 = 2;
        public const int F03 = 3;
        public const int F04 = 4;
        public const int F05 = 5;
        public const int F06 = 6;
        public const int L01 = 201;
        public const int L02 = 202;
        public const int L04 = 204;
        public const int L07 = 207;
        public const int L10 = 210;
        public const int L14 = 214;
        public const int L15 = 215;

        public static IReadOnlyList<int> AllIds => new[] { F01, F02, F03, F04, F05, F06, L01, L02, L04, L07, L10, L14, L15 };

        public static IReadOnlyList<CardDefinition> All()
        {
            return new[]
            {
                Card(F01, Plan.Free, CardType.Active, Rarity.N, false, false,
                    Face(Consume(4), Param(9)),
                    Face(Consume(3), Param(14))),
                Card(F02, Plan.Free, CardType.Active, Rarity.N, false, false,
                    Face(Consume(3), Param(2), Shield(2)),
                    Face(Consume(3), Param(6), Shield(4))),
                Card(F03, Plan.Free, CardType.Mental, Rarity.N, true, false,
                    Face(Consume(0), Shield(4)),
                    Face(Consume(0), Shield(7))),
                Card(F04, Plan.Free, CardType.Mental, Rarity.R, false, false,
                    Face(Consume(0), Shield(2), Turns(StatusKind.CostHalf, 2)),
                    Face(Consume(0), Shield(2), Turns(StatusKind.CostHalf, 3))),
                Card(F05, Plan.Free, CardType.Mental, Rarity.R, true, true,
                    Face(Consume(0), Shield(3), StackIf(StatusKind.CostReduce, 1, HpAtLeast(50))),
                    Face(Consume(0), Shield(6), StackIf(StatusKind.CostReduce, 1, HpAtLeast(50)))),
                Card(F06, Plan.Free, CardType.Mental, Rarity.SR, true, true,
                    Face(HpDirect(1), Uses(1), Draw(2)),
                    Face(Consume(0), Uses(1), Draw(2), Turns(StatusKind.CostHalf, 1))),

                Card(L01, Plan.Logic, CardType.Mental, Rarity.N, false, false,
                    Face(Consume(2), Shield(1), Impression(2)),
                    Face(Consume(2), Shield(2), Impression(3))),
                Card(L02, Plan.Logic, CardType.Mental, Rarity.N, true, false,
                    FaceWithCondition(Consume(2), ImpressionAtLeast(1), Impression(4)),
                    FaceWithCondition(Consume(2), ImpressionAtLeast(1), Impression(5))),
                Card(L04, Plan.Logic, CardType.Active, Rarity.N, true, false,
                    Face(Consume(5), Impression(2), RatioOfImpression(100)),
                    Face(Consume(5), Impression(3), RatioOfImpression(120))),
                Card(L07, Plan.Logic, CardType.Active, Rarity.R, false, false,
                    Face(Consume(4), Param(7), Impression(3)),
                    Face(Consume(4), Param(9), Impression(4))),
                Card(L10, Plan.Logic, CardType.Active, Rarity.R, true, false,
                    Face(Consume(5), RatioOfImpression(150)),
                    Face(Consume(5), RatioOfImpression(200))),
                Card(L14, Plan.Logic, CardType.Mental, Rarity.SR, true, true,
                    Face(Consume(4), Impression(3), OnMentalUse(Impression(1))),
                    Face(Consume(4), Impression(5), OnMentalUse(Impression(1)))),
                Card(L15, Plan.Logic, CardType.Active, Rarity.SSR, true, true,
                    Face(Consume(6), Impression(5), RatioOfImpression(100)),
                    Face(Consume(6), Impression(6), RatioOfImpression(170))),
            };
        }
    }
}

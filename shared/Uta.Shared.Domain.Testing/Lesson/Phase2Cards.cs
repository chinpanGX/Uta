using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using static Uta.Shared.Domain.Tests.Lesson.TestCards;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>
    /// フェーズ2のカード（lesson-card.md 14章）。ID はテスト用の値。
    /// </summary>
    public static class Phase2Cards
    {
        public const int F07 = 7;
        public const int L03 = 203;
        public const int L05 = 205;
        public const int L06 = 206;
        public const int L08 = 208;
        public const int L09 = 209;
        public const int L11 = 211;
        public const int L12 = 212;
        public const int L13 = 213;

        public static IReadOnlyList<int> AllIds => new[] { F07, L03, L05, L06, L08, L09, L11, L12, L13 };

        public static IReadOnlyList<CardDefinition> All()
        {
            return new[]
            {
                Card(F07, Plan.Free, CardType.Mental, Rarity.SR, true, false,
                    Face(Consume(0), Shield(11), Turns(StatusKind.ShieldGainBlock, 2), Turns(StatusKind.CostHalf, 3)),
                    Face(Consume(0), Shield(13), Turns(StatusKind.ShieldGainBlock, 2), Turns(StatusKind.CostHalf, 4))),
                Card(L03, Plan.Logic, CardType.Mental, Rarity.N, false, false,
                    Face(Consume(2), Shield(1), Status(StatusKind.Motivation, 2)),
                    Face(Consume(2), Shield(2), Status(StatusKind.Motivation, 3))),
                Card(L05, Plan.Logic, CardType.Active, Rarity.N, true, false,
                    Face(HpDirect(5), ParamByShield(100)),
                    Face(HpDirect(3), ParamByShield(110))),
                Card(L06, Plan.Logic, CardType.Active, Rarity.N, true, false,
                    Face(Consume(2), Shield(3), ParamByMotivation(120)),
                    Face(Consume(2), Shield(6), ParamByMotivation(150))),
                Card(L08, Plan.Logic, CardType.Active, Rarity.R, true, false,
                    Face(HpDirect(4), Status(StatusKind.Motivation, 3), ParamByShield(60)),
                    Face(HpDirect(4), Status(StatusKind.Motivation, 4), ParamByShield(80))),
                Card(L09, Plan.Logic, CardType.Mental, Rarity.R, false, false,
                    Face(Consume(0), Shield(6)),
                    Face(Consume(0), Shield(8))),
                Card(L11, Plan.Logic, CardType.Active, Rarity.R, true, false,
                    Face(HpDirect(4), ParamByShield(110)),
                    Face(HpDirect(3), ParamByShield(120))),
                Card(L12, Plan.Logic, CardType.Mental, Rarity.SR, true, false,
                    Face(Consume(5), Impression(4), Status(StatusKind.Motivation, 3)),
                    Face(Consume(5), Impression(5), Status(StatusKind.Motivation, 4)),
                    startInHand: true),
                Card(L13, Plan.Logic, CardType.Active, Rarity.SR, true, false,
                    Face(HpDirect(3), ParamByShield(130), ScaleShield(50)),
                    Face(HpDirect(3), ParamByShield(180), ScaleShield(50))),
            };
        }
    }
}

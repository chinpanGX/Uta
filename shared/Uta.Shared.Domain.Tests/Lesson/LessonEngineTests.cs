using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class LessonEngineTests
    {
        private static readonly TestCardCatalog Catalog = new TestCardCatalog();
        private static readonly LessonEngine Engine = new LessonEngine(Catalog);

        private static readonly LessonSetup NormalLesson = TestStates.NormalLesson;

        [Fact]
        public void Start_NineCards_HandThreeAndDrawPileSix()
        {
            var state = new LessonState(NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);

            Engine.Start(state, TestStates.NewRandom());

            Assert.Equal(LessonPhase.Player, state.Phase);
            Assert.Equal(1, state.Turn);
            Assert.Equal(3, state.Zones.Hand.Count);
            Assert.Equal(6, state.Zones.DrawPile.Count);
            Assert.Equal(1, state.UsesLeft);
        }

        [Fact]
        public void Start_SevenStartInHandCards_HandFiveAndRestInDrawPile()
        {
            var ids = Enumerable.Repeat(SyntheticCards.StartInHandDummy, 7).Concat(Enumerable.Repeat(Phase1Cards.F01, 3));
            var state = new LessonState(NormalLesson, TestStates.Deck(ids), 30, 30);

            Engine.Start(state, TestStates.NewRandom());

            Assert.Equal(5, state.Zones.Hand.Count);
            Assert.All(state.Zones.Hand, id => Assert.Equal(SyntheticCards.StartInHandDummy, state.GetCard(id).CardId));
            Assert.Equal(5, state.Zones.DrawPile.Count);
        }

        [Fact]
        public void UseCard_F01_PaysHpAndEndsTurn()
        {
            var state = new LessonState(NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);
            Engine.Start(state, TestStates.NewRandom());

            var result = Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), TestStates.NewRandom());

            Assert.Equal(LessonApplyResult.Accepted, result);
            Assert.Equal(26, state.Hp);
            Assert.Equal(9, state.ParamGained);
            Assert.Equal(2, state.Turn);
            Assert.Equal(3, state.Zones.Hand.Count);
            Assert.Equal(3, state.Zones.DrawPile.Count);
            Assert.Equal(3, state.Zones.Discard.Count);
        }

        [Fact]
        public void UseCard_ConsumeCost_PaysShieldBeforeHp()
        {
            var state = new LessonState(NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.F02, 9)), 30, 30);
            var random = TestStates.NewRandom();
            Engine.Start(state, random);

            Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            Assert.Equal(27, state.Hp);
            Assert.Equal(2, state.Shield);

            // 消費3 を元気2 → 体力1 の順に払い、そのあと元気+2
            Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            Assert.Equal(26, state.Hp);
            Assert.Equal(2, state.Shield);
            Assert.Equal(4, state.ParamGained);
        }

        [Fact]
        public void CostHalf_GrantedInPlayerPhase_LastsFullTurns()
        {
            var state = new LessonState(NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.F04, 9)), 30, 30);
            var random = TestStates.NewRandom();
            Engine.Start(state, random);

            // プレイヤー操作中に初めて付与 → 1ターン目の終了時には減らない（11章）
            Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            Assert.Equal(2, state.Statuses.Get(StatusKind.CostHalf));

            Engine.Apply(state, LessonCommand.Skip(), random);
            Assert.Equal(1, state.Statuses.Get(StatusKind.CostHalf));

            Engine.Apply(state, LessonCommand.Skip(), random);
            Assert.Equal(0, state.Statuses.Get(StatusKind.CostHalf));
        }

        [Fact]
        public void Impression_RaisesParamAtTurnEndAndDecaysAtTurnStart()
        {
            var state = new LessonState(NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.L01, 9)), 30, 30);
            var random = TestStates.NewRandom();
            Engine.Start(state, random);

            Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            Assert.Equal(28, state.Hp);
            Assert.Equal(2, state.ParamGained); // E3 で好印象2
            Assert.Equal(1, state.Statuses.Get(StatusKind.Impression)); // T2 で1減

            Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), random);
            Assert.Equal(27, state.Hp); // 元気1 + 体力1
            Assert.Equal(5, state.ParamGained); // 2 + 好印象3
        }

        [Fact]
        public void CanUse_L02WithoutImpression_UseConditionNotMet()
        {
            var state = new LessonState(NormalLesson, TestStates.Deck(Enumerable.Repeat(Phase1Cards.L02, 9)), 30, 30);
            Engine.Start(state, TestStates.NewRandom());

            Assert.Equal(UseCheckResult.UseConditionNotMet, Engine.CanUse(state, state.Zones.Hand[0]));
            Assert.Equal(LessonApplyResult.UseConditionNotMet, Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), TestStates.NewRandom()));
        }

        [Fact]
        public void UseCard_L10_ParamFromImpressionRatio()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.L10, Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new[] { 2 }, hp: 30, statuses: new Dictionary<StatusKind, int> { { StatusKind.Impression, 4 } });

            Engine.Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());

            // ceil(4 × 1.5) = 6、ターン終了時に好印象4
            Assert.Equal(10, state.ParamGained);
            Assert.Equal(25, state.Hp);
            Assert.Contains(1, state.Zones.Exiled);
        }

        [Fact]
        public void Persistent_L14_ReactsToLaterMentalCardButNotItself()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.L14, Phase1Cards.L01 }, hand: new[] { 1, 2 }, drawPile: new int[0], hp: 30, usesLeft: 2);

            Engine.Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Equal(3, state.Statuses.Get(StatusKind.Impression));
            Assert.Single(state.Persistents);

            Engine.Apply(state, LessonCommand.UseCard(2), TestStates.NewRandom());

            // 3 + 持続効果1 + L01 の2 = 6 → ターン終了時にパラメータ+6
            Assert.Equal(6, state.ParamGained);
        }

        [Theory]
        [InlineData(15, 1)]
        [InlineData(14, 0)]
        public void UseCard_F05_CostReduceOnlyWhenHpAtLeastHalf(int hp, int expectedReduce)
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F05 }, hand: new[] { 1 }, drawPile: new int[0], hp: hp);

            Engine.Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());

            Assert.Equal(expectedReduce, state.Statuses.Get(StatusKind.CostReduce));
        }

        [Fact]
        public void UseCard_F06_AddsUseAndDrawsTwo()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F06, Phase1Cards.F01, Phase1Cards.F01, Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new[] { 2, 3, 4 }, hp: 30);

            Engine.Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());

            Assert.Equal(29, state.Hp);
            Assert.Equal(LessonPhase.Player, state.Phase);
            Assert.Equal(1, state.UsesLeft);
            Assert.Equal(new[] { 2, 3 }, state.Zones.Hand);
            Assert.Equal(new[] { 4 }, state.Zones.DrawPile);
        }

        [Fact]
        public void Skip_RecoversHpUpToMax()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 29);

            Engine.Apply(state, LessonCommand.Skip(), TestStates.NewRandom());

            Assert.Equal(30, state.Hp);
            Assert.Equal(2, state.Turn);
        }

        [Fact]
        public void UseCard_ReachesPerfect_StopsAtPerfectAndFinishes()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, paramGained: 75);

            Engine.Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());

            Assert.Equal(80, state.ParamGained);
            Assert.True(state.IsFinished);
        }

        [Fact]
        public void Skip_LastTurn_Finishes()
        {
            var setup = new LessonSetup(LessonKind.Normal, ParamKind.Singing, 1, 40, 80);
            var state = new LessonState(setup, TestStates.Deck(Enumerable.Repeat(Phase1Cards.F01, 5)), 30, 30);
            Engine.Start(state, TestStates.NewRandom());

            Engine.Apply(state, LessonCommand.Skip(), TestStates.NewRandom());

            Assert.True(state.IsFinished);
            Assert.Equal(LessonApplyResult.NotPlayerPhase, Engine.Apply(state, LessonCommand.Skip(), TestStates.NewRandom()));
        }

        [Fact]
        public void Draw_DrawPileRunsOut_ReshufflesDiscardAndContinues()
        {
            var state = TestStates.PlayerState(
                new[] { Phase1Cards.F01, Phase1Cards.F01, Phase1Cards.F01, Phase1Cards.F01 },
                hand: new[] { 4 },
                drawPile: new[] { 1 },
                hp: 30,
                discard: new[] { 2, 3 });

            Engine.Apply(state, LessonCommand.Skip(), TestStates.NewRandom());

            // 1 を引いて山札が0枚 → 捨札（2, 3, 手札から捨てた 4）をシャッフル → 残り2枚を引く
            Assert.Equal(3, state.Zones.Hand.Count);
            Assert.Equal(1, state.Zones.Hand[0]);
            Assert.Single(state.Zones.DrawPile);
            Assert.Empty(state.Zones.Discard);
        }

        [Fact]
        public void UseDrink_Phase1_NotSupported()
        {
            var state = TestStates.PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30);

            Assert.Equal(LessonApplyResult.NotSupported, Engine.Apply(state, LessonCommand.UseDrink(0), TestStates.NewRandom()));
        }

        [Fact]
        public void Replay_SameSeedAndCommands_SameResult()
        {
            var first = RunLesson(1, 0, null);
            var second = RunLesson(1, 0, null);

            Assert.Equal(first, second);
        }

        [Fact]
        public void Replay_DifferentAttempt_DifferentResult()
        {
            Assert.NotEqual(RunLesson(1, 0, null), RunLesson(1, 1, null));
        }

        [Fact]
        public void Replay_RestoredMidway_SameResultAsContinuousRun()
        {
            // 3ターン目で保存・復元しても、通しで進めたときと同じ結果になる（produce.md 10.5・10.6）
            Assert.Equal(RunLesson(3, 0, null), RunLesson(3, 0, 3));
        }

        [Fact]
        public void Validator_Phase1Cards_NoErrors()
        {
            foreach (var id in Phase1Cards.AllIds)
            {
                Assert.Empty(CardDefinitionValidator.Validate(Catalog.Get(id)));
            }
        }

        [Fact]
        public void Validator_UnsupportedOp_ReportsError()
        {
            var face = new CardFace(new CostDef[0], new ConditionDef[0], new[] { new EffectDef(EffectOp.ScaleShield, value: 50) });
            var card = new CardDefinition(999, Plan.Logic, CardType.Active, Rarity.SR, 1, true, false, false, face, null);

            Assert.Single(CardDefinitionValidator.Validate(card));
        }

        /// <summary>
        /// 13種のカードの混ざったデッキで、使えるカードがあれば手札の先頭から使い、なければスキップする。
        /// restoreAtTurn を渡すと、そのターンの入力待ちで状態と乱数を作り直してから続ける。
        /// </summary>
        private static string RunLesson(ulong seed, int attempt, int? restoreAtTurn)
        {
            var setup = new LessonSetup(LessonKind.Normal, ParamKind.Rhythm, 7, 1000, 2000);
            var ids = Phase1Cards.AllIds.Concat(new[] { Phase1Cards.F01, Phase1Cards.L01, Phase1Cards.L07 });
            var state = new LessonState(setup, TestStates.Deck(ids), 40, 40);
            var random = RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Lesson, 3, attempt));
            var log = new StringBuilder();

            Engine.Start(state, random);
            while (!state.IsFinished)
            {
                if (restoreAtTurn.HasValue && state.Turn == restoreAtTurn.Value)
                {
                    state = TestStates.Copy(state);
                    random = new Xoshiro256StarStar(random.S0, random.S1, random.S2, random.S3, random.DrawCount);
                    restoreAtTurn = null;
                }

                var usable = state.Zones.Hand.FirstOrDefault(id => Engine.CanUse(state, id) == UseCheckResult.Ok);
                var command = usable != 0 ? LessonCommand.UseCard(usable) : LessonCommand.Skip();
                Assert.Equal(LessonApplyResult.Accepted, Engine.Apply(state, command, random));
                log.Append(string.Join("\n", LessonStateComparer.Describe(state))).Append('\n');
            }

            return log.ToString();
        }
    }
}

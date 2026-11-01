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
        private static readonly Phase1Cards Catalog = new Phase1Cards();
        private static readonly LessonEngine Engine = new LessonEngine(Catalog);

        private static readonly LessonSetup NormalLesson = new LessonSetup(LessonKind.Normal, ParamKind.Singing, 6, 40, 80);

        [Fact]
        public void Start_NineCards_HandThreeAndDrawPileSix()
        {
            var state = new LessonState(NormalLesson, Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);

            Engine.Start(state, NewRandom());

            Assert.Equal(LessonPhase.Player, state.Phase);
            Assert.Equal(1, state.Turn);
            Assert.Equal(3, state.Zones.Hand.Count);
            Assert.Equal(6, state.Zones.DrawPile.Count);
            Assert.Equal(1, state.UsesLeft);
        }

        [Fact]
        public void Start_SevenStartInHandCards_HandFiveAndRestInDrawPile()
        {
            var ids = Enumerable.Repeat(Phase1Cards.StartInHandDummy, 7).Concat(Enumerable.Repeat(Phase1Cards.F01, 3));
            var state = new LessonState(NormalLesson, Deck(ids), 30, 30);

            Engine.Start(state, NewRandom());

            Assert.Equal(5, state.Zones.Hand.Count);
            Assert.All(state.Zones.Hand, id => Assert.Equal(Phase1Cards.StartInHandDummy, state.GetCard(id).CardId));
            Assert.Equal(5, state.Zones.DrawPile.Count);
        }

        [Fact]
        public void UseCard_F01_PaysHpAndEndsTurn()
        {
            var state = new LessonState(NormalLesson, Deck(Enumerable.Repeat(Phase1Cards.F01, 9)), 30, 30);
            Engine.Start(state, NewRandom());

            var result = Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), NewRandom());

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
            var state = new LessonState(NormalLesson, Deck(Enumerable.Repeat(Phase1Cards.F02, 9)), 30, 30);
            var random = NewRandom();
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
            var state = new LessonState(NormalLesson, Deck(Enumerable.Repeat(Phase1Cards.F04, 9)), 30, 30);
            var random = NewRandom();
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
            var state = new LessonState(NormalLesson, Deck(Enumerable.Repeat(Phase1Cards.L01, 9)), 30, 30);
            var random = NewRandom();
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
            var state = new LessonState(NormalLesson, Deck(Enumerable.Repeat(Phase1Cards.L02, 9)), 30, 30);
            Engine.Start(state, NewRandom());

            Assert.Equal(UseCheckResult.UseConditionNotMet, Engine.CanUse(state, state.Zones.Hand[0]));
            Assert.Equal(LessonApplyResult.UseConditionNotMet, Engine.Apply(state, LessonCommand.UseCard(state.Zones.Hand[0]), NewRandom()));
        }

        [Fact]
        public void UseCard_L10_ParamFromImpressionRatio()
        {
            var state = PlayerState(new[] { Phase1Cards.L10, Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new[] { 2 }, hp: 30, impression: 4);

            Engine.Apply(state, LessonCommand.UseCard(1), NewRandom());

            // ceil(4 × 1.5) = 6、ターン終了時に好印象4
            Assert.Equal(10, state.ParamGained);
            Assert.Equal(25, state.Hp);
            Assert.Contains(1, state.Zones.Exiled);
        }

        [Fact]
        public void Persistent_L14_ReactsToLaterMentalCardButNotItself()
        {
            var state = PlayerState(new[] { Phase1Cards.L14, Phase1Cards.L01 }, hand: new[] { 1, 2 }, drawPile: new int[0], hp: 30, usesLeft: 2);

            Engine.Apply(state, LessonCommand.UseCard(1), NewRandom());
            Assert.Equal(3, state.Statuses.Get(StatusKind.Impression));
            Assert.Single(state.Persistents);

            Engine.Apply(state, LessonCommand.UseCard(2), NewRandom());

            // 3 + 持続効果1 + L01 の2 = 6 → ターン終了時にパラメータ+6
            Assert.Equal(6, state.ParamGained);
        }

        [Theory]
        [InlineData(15, 1)]
        [InlineData(14, 0)]
        public void UseCard_F05_CostReduceOnlyWhenHpAtLeastHalf(int hp, int expectedReduce)
        {
            var state = PlayerState(new[] { Phase1Cards.F05 }, hand: new[] { 1 }, drawPile: new int[0], hp: hp);

            Engine.Apply(state, LessonCommand.UseCard(1), NewRandom());

            Assert.Equal(expectedReduce, state.Statuses.Get(StatusKind.CostReduce));
        }

        [Fact]
        public void UseCard_F06_AddsUseAndDrawsTwo()
        {
            var state = PlayerState(new[] { Phase1Cards.F06, Phase1Cards.F01, Phase1Cards.F01, Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new[] { 2, 3, 4 }, hp: 30);

            Engine.Apply(state, LessonCommand.UseCard(1), NewRandom());

            Assert.Equal(29, state.Hp);
            Assert.Equal(LessonPhase.Player, state.Phase);
            Assert.Equal(1, state.UsesLeft);
            Assert.Equal(new[] { 2, 3 }, state.Zones.Hand);
            Assert.Equal(new[] { 4 }, state.Zones.DrawPile);
        }

        [Fact]
        public void Skip_RecoversHpUpToMax()
        {
            var state = PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 29);

            Engine.Apply(state, LessonCommand.Skip(), NewRandom());

            Assert.Equal(30, state.Hp);
            Assert.Equal(2, state.Turn);
        }

        [Fact]
        public void UseCard_ReachesPerfect_StopsAtPerfectAndFinishes()
        {
            var state = PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30, paramGained: 75);

            Engine.Apply(state, LessonCommand.UseCard(1), NewRandom());

            Assert.Equal(80, state.ParamGained);
            Assert.True(state.IsFinished);
        }

        [Fact]
        public void Skip_LastTurn_Finishes()
        {
            var setup = new LessonSetup(LessonKind.Normal, ParamKind.Singing, 1, 40, 80);
            var state = new LessonState(setup, Deck(Enumerable.Repeat(Phase1Cards.F01, 5)), 30, 30);
            Engine.Start(state, NewRandom());

            Engine.Apply(state, LessonCommand.Skip(), NewRandom());

            Assert.True(state.IsFinished);
            Assert.Equal(LessonApplyResult.NotPlayerPhase, Engine.Apply(state, LessonCommand.Skip(), NewRandom()));
        }

        [Fact]
        public void Draw_DrawPileRunsOut_ReshufflesDiscardAndContinues()
        {
            var state = PlayerState(
                new[] { Phase1Cards.F01, Phase1Cards.F01, Phase1Cards.F01, Phase1Cards.F01 },
                hand: new[] { 4 },
                drawPile: new[] { 1 },
                hp: 30,
                discard: new[] { 2, 3 });

            Engine.Apply(state, LessonCommand.Skip(), NewRandom());

            // 1 を引いて山札が0枚 → 捨札（2, 3, 手札から捨てた 4）をシャッフル → 残り2枚を引く
            Assert.Equal(3, state.Zones.Hand.Count);
            Assert.Equal(1, state.Zones.Hand[0]);
            Assert.Single(state.Zones.DrawPile);
            Assert.Empty(state.Zones.Discard);
        }

        [Fact]
        public void UseDrink_Phase1_NotSupported()
        {
            var state = PlayerState(new[] { Phase1Cards.F01 }, hand: new[] { 1 }, drawPile: new int[0], hp: 30);

            Assert.Equal(LessonApplyResult.NotSupported, Engine.Apply(state, LessonCommand.UseDrink(0), NewRandom()));
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
            foreach (var id in Phase1Cards.AllPhase1Ids)
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
            var ids = Phase1Cards.AllPhase1Ids.Concat(new[] { Phase1Cards.F01, Phase1Cards.L01, Phase1Cards.L07 });
            var state = new LessonState(setup, Deck(ids), 40, 40);
            var random = RandomStreams.Create(seed, new RandomStreamKey(RandomStreamKind.Lesson, 3, attempt));
            var log = new StringBuilder();

            Engine.Start(state, random);
            while (!state.IsFinished)
            {
                if (restoreAtTurn.HasValue && state.Turn == restoreAtTurn.Value)
                {
                    state = Copy(state);
                    random = new Xoshiro256StarStar(random.S0, random.S1, random.S2, random.S3, random.DrawCount);
                    restoreAtTurn = null;
                }

                var usable = state.Zones.Hand.FirstOrDefault(id => Engine.CanUse(state, id) == UseCheckResult.Ok);
                var command = usable != 0 ? LessonCommand.UseCard(usable) : LessonCommand.Skip();
                Assert.Equal(LessonApplyResult.Accepted, Engine.Apply(state, command, random));
                log.Append(Summarize(state)).Append('\n');
            }

            return log.ToString();
        }

        /// <summary>保存して作り直したのと同じように、全部の値を写した別の状態を作る。</summary>
        private static LessonState Copy(LessonState s)
        {
            var values = new int[StatusSet.KindCount];
            var skips = new bool[StatusSet.KindCount];
            for (var i = 0; i < StatusSet.KindCount; i++)
            {
                values[i] = s.Statuses.Get((StatusKind)i);
                skips[i] = s.Statuses.IsSkipNextDecrement((StatusKind)i);
            }

            return new LessonState(
                s.Setup,
                s.Deck.Select(c => new CardInstance(c.InstanceId, c.CardId, c.Enhanced)).ToList(),
                new Zones(s.Zones.DrawPile.ToList(), s.Zones.Hand.ToList(), s.Zones.Discard.ToList(), s.Zones.Exiled.ToList()),
                new StatusSet(values, skips),
                s.ParamBonuses.Select(b => new ParamBonusEntry(b.Percent, b.RemainingTurns, b.SkipNextDecrement)).ToList(),
                s.Scheduled.Select(x => new ScheduledEffect(x.RemainingTurns, x.Effects, x.GrantOrder)).ToList(),
                s.Persistents.Select(p => new PersistentEffect(p.Trigger, p.Effects, p.RemainingTurns, p.RemainingCount, p.GrantOrder, p.SkipNextDecrement)).ToList(),
                s.LessonEnhanced.ToList(),
                s.Turn,
                s.Phase,
                s.Hp,
                s.MaxHp,
                s.Shield,
                s.UsesLeft,
                s.ParamGained,
                s.NextGrantOrder);
        }

        private static string Summarize(LessonState s)
        {
            var statuses = string.Join(",", Enumerable.Range(0, StatusSet.KindCount).Select(i => s.Statuses.Get((StatusKind)i)));
            return string.Format(
                "t{0} {1} hp{2} g{3} p{4} u{5} pile[{6}] hand[{7}] dis[{8}] ex[{9}] st[{10}] per{11}",
                s.Turn,
                s.Phase,
                s.Hp,
                s.Shield,
                s.ParamGained,
                s.UsesLeft,
                string.Join(",", s.Zones.DrawPile),
                string.Join(",", s.Zones.Hand),
                string.Join(",", s.Zones.Discard),
                string.Join(",", s.Zones.Exiled),
                statuses,
                s.Persistents.Count);
        }

        /// <summary>カード ID の並びから、インスタンス ID を 1 から振ったデッキを作る。</summary>
        private static List<CardInstance> Deck(IEnumerable<int> cardIds)
        {
            return cardIds.Select((cardId, index) => new CardInstance(index + 1, cardId, false)).ToList();
        }

        /// <summary>1ターン目のプレイヤー操作の入力待ちの状態を、復元用のコンストラクタで作る。</summary>
        private static LessonState PlayerState(
            int[] cardIds,
            int[] hand,
            int[] drawPile,
            int hp,
            int impression = 0,
            int usesLeft = 1,
            int paramGained = 0,
            int[] discard = null)
        {
            var values = new int[StatusSet.KindCount];
            values[(int)StatusKind.Impression] = impression;

            return new LessonState(
                NormalLesson,
                Deck(cardIds),
                new Zones(drawPile, hand, discard ?? new int[0], new int[0]),
                new StatusSet(values, new bool[StatusSet.KindCount]),
                new ParamBonusEntry[0],
                new ScheduledEffect[0],
                new PersistentEffect[0],
                new int[0],
                1,
                LessonPhase.Player,
                hp,
                30,
                0,
                usesLeft,
                paramGained,
                0);
        }

        private static Xoshiro256StarStar NewRandom()
        {
            return RandomStreams.Create(1, new RandomStreamKey(RandomStreamKind.Lesson, 1, 0));
        }
    }
}

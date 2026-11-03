using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Tests.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceSnapshotTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        private static ProduceStartInput Input(int scenario, int idol)
        {
            var supports = scenario == 2 && idol == 1
                ? new[] { new SupportCardInput(1, 5, 0) }
                : new SupportCardInput[0];
            return new ProduceStartInput(scenario, idol, 1, supports);
        }

        private static ProduceState RoundTrip(ProduceState state)
        {
            return ProduceSnapshot.From(state, Catalog.Cards).ToState(Catalog);
        }

        private static void AssertSameState(ProduceState expected, ProduceState actual)
        {
            Assert.Empty(ProduceStateComparer.Diff(expected, actual));
        }

        public static IEnumerable<object[]> Cases()
        {
            foreach (var seed in new[] { 1, 2, 3 })
            {
                foreach (var idol in new[] { 1, 2 })
                {
                    yield return new object[] { 1, idol, seed };
                }

                foreach (var idol in new[] { 1, 2, 3, 4 })
                {
                    yield return new object[] { 2, idol, seed };
                }
            }
        }

        public static IEnumerable<object[]> RetryCases()
        {
            foreach (var c in Cases())
            {
                yield return new[] { c[0], c[1], c[2], false };
                yield return new[] { c[0], c[1], c[2], true };
            }
        }

        [Fact]
        public void From_ToState_AfterStart_SameState()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = engine.Start(Input(1, 1), streams);

            AssertSameState(state, RoundTrip(state));
        }

        [Theory]
        [MemberData(nameof(RetryCases))]
        public void RoundTrip_EveryOperation_SameState(int scenario, int idol, int seed, bool retryExam)
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource((ulong)seed);
            var state = engine.Start(Input(scenario, idol), streams);
            AssertSameState(state, RoundTrip(state));

            var count = 0;
            while (state.Phase != ProducePhase.Finished)
            {
                var command = ProduceBot.Next(engine, Catalog, state, true, retryExam);
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
                AssertSameState(state, RoundTrip(state));
                count++;
                Assert.True(count < 3000);
            }
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void Resume_EveryOperation_SameFinalState(int scenario, int idol, int seed)
        {
            var engine = new ProduceEngine(Catalog);

            var streamsA = new TestRandomStreamSource((ulong)seed);
            var a = engine.Start(Input(scenario, idol), streamsA);
            ProduceBot.Run(engine, Catalog, a, streamsA, playCards: true, retryExam: false);

            var streamsB = new TestRandomStreamSource((ulong)seed);
            var b = engine.Start(Input(scenario, idol), streamsB);
            while (b.Phase != ProducePhase.Finished)
            {
                var command = ProduceBot.Next(engine, Catalog, b, true, false);
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(b, command, streamsB));
                b = RoundTrip(b);
            }

            AssertSameState(a, b);
            Assert.Equal(a.Operations.Count, b.Operations.Count);
        }

        [Fact]
        public void RoundTrip_FinishedState_KeepsResult()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = engine.Start(Input(1, 1), streams);
            ProduceBot.Run(engine, Catalog, state, streams, playCards: true, retryExam: false);

            Assert.NotNull(state.Result);
            var restored = RoundTrip(state);
            Assert.NotNull(restored.Result);
            AssertSameState(state, restored);
        }

        [Fact]
        public void RoundTrip_ExamResultPhase_KeepsBackupAndReports()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = engine.Start(Input(2, 1), streams);
            ProduceBot.RunUntil(engine, Catalog, state, streams, s => s.Phase == ProducePhase.ExamResult, retryExam: true);

            Assert.Equal(ProducePhase.ExamResult, state.Phase);
            Assert.NotNull(state.ExamBackup);
            Assert.NotNull(state.LastExam);
            var restored = RoundTrip(state);
            Assert.NotNull(restored.ExamBackup);
            Assert.NotNull(restored.LastExam);
            AssertSameState(state, restored);
        }

        [Fact]
        public void RoundTrip_LessonInProgress_KeepsZonesAndOrder()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var state = engine.Start(Input(1, 1), streams);
            ProduceBot.RunUntil(engine, Catalog, state, streams, s => s.Phase == ProducePhase.Lesson && s.Lesson.Turn >= 2);

            Assert.Equal(ProducePhase.Lesson, state.Phase);
            var restored = RoundTrip(state);
            AssertSameState(state, restored);
            Assert.Equal(state.Lesson.Zones.DrawPile.ToArray(), restored.Lesson.Zones.DrawPile.ToArray());
            Assert.Equal(state.Lesson.Zones.Hand.ToArray(), restored.Lesson.Zones.Hand.ToArray());
        }

        private static (TestCardCatalog Catalog, int CardId) FindPersistentCard(bool enhancedFace)
        {
            var catalog = new TestCardCatalog();
            foreach (var id in catalog.CardIds)
            {
                var def = catalog.Get(id);
                var face = enhancedFace ? def.Enhanced : def.Base;
                if (face != null && face.Effects.Any(e => e.Op == EffectOp.AddPersistent))
                {
                    return (catalog, id);
                }
            }

            throw new InvalidOperationException("持続効果のカードがありません。");
        }

        private static LessonState PersistentState(TestCardCatalog catalog, int cardId, bool enhanced)
        {
            var state = TestStates.PlayerState(new[] { cardId }, new[] { 1 }, new int[0], 30, usesLeft: 1, enhanced: enhanced ? new[] { 1 } : null);
            new LessonEngine(catalog).Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom());
            Assert.Single(state.Persistents);
            return state;
        }

        [Fact]
        public void LessonSnapshot_Persistent_RoundTripsOrigin()
        {
            var (catalog, cardId) = FindPersistentCard(false);
            var state = PersistentState(catalog, cardId, false);

            var snapshot = LessonSnapshot.From(state, catalog);
            var restored = snapshot.ToState(catalog);

            Assert.Empty(LessonStateComparer.Diff(state, restored));
            Assert.Equal(cardId, snapshot.Persistents[0].CardId);
            Assert.False(snapshot.Persistents[0].Enhanced);
        }

        [Fact]
        public void LessonSnapshot_PersistentOfEnhancedFace_RoundTripsOrigin()
        {
            var (catalog, cardId) = FindPersistentCard(true);
            var state = PersistentState(catalog, cardId, true);

            var snapshot = LessonSnapshot.From(state, catalog);
            var restored = snapshot.ToState(catalog);

            Assert.Empty(LessonStateComparer.Diff(state, restored));
            Assert.True(snapshot.Persistents[0].Enhanced);
        }

        [Fact]
        public void LessonSnapshot_Scheduled_Throws()
        {
            var catalog = new TestCardCatalog();
            var s = TestStates.PlayerState(new[] { catalog.CardIds[0] }, new[] { 1 }, new int[0], 30);
            var withScheduled = new LessonState(
                s.Setup,
                s.Deck,
                s.Zones,
                s.Statuses,
                s.ParamBonuses,
                new[] { new ScheduledEffect(1, new[] { new EffectDef(EffectOp.GainParam, value: 1) }, 0) },
                s.Persistents,
                s.LessonEnhanced,
                s.Turn,
                s.Phase,
                s.Hp,
                s.MaxHp,
                s.Shield,
                s.UsesLeft,
                s.ParamGained,
                s.NextGrantOrder);

            Assert.Throws<NotSupportedException>(() => LessonSnapshot.From(withScheduled, catalog));
        }

        private static (TestCardCatalog Catalog, int CardId, LessonSnapshot Snapshot) PersistentSnapshotSource()
        {
            var (catalog, cardId) = FindPersistentCard(false);
            var state = PersistentState(catalog, cardId, false);
            return (catalog, cardId, LessonSnapshot.From(state, catalog));
        }

        private static ProduceSnapshot StartSnapshot()
        {
            var engine = new ProduceEngine(Catalog);
            var state = engine.Start(Input(1, 1), new TestRandomStreamSource(1));
            return ProduceSnapshot.From(state, Catalog.Cards);
        }

        [Fact]
        public void ToState_ProduceParametersWrongLength_Throws()
        {
            var snapshot = StartSnapshot();
            snapshot.Parameters = new int[2];
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(Catalog));
        }

        [Fact]
        public void ToState_ProduceArrayNull_Throws()
        {
            var snapshot = StartSnapshot();
            snapshot.Deck = null;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(Catalog));
        }

        [Fact]
        public void ToState_ProduceDeckElementNull_Throws()
        {
            var snapshot = StartSnapshot();
            snapshot.Deck[0] = null;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(Catalog));
        }

        [Fact]
        public void ToState_ExamBackupDeckNull_Throws()
        {
            var snapshot = StartSnapshot();
            snapshot.ExamBackup = new ExamBackupSnapshot { Parameters = new int[3], Hp = 1, Deck = null, NextInstanceId = 1 };
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(Catalog));
        }

        [Fact]
        public void ToState_ExamBackupParametersNull_Throws()
        {
            var snapshot = StartSnapshot();
            snapshot.ExamBackup = new ExamBackupSnapshot { Parameters = null, Hp = 1, Deck = new CardInstanceSnapshot[0], NextInstanceId = 1 };
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(Catalog));
        }

        [Fact]
        public void ToState_StatusValuesWrongLength_Throws()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            snapshot.StatusValues = new int[1];
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_LessonArrayNull_Throws()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            snapshot.Hand = null;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_ExamTurnLengthsDiffer_Throws()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            snapshot.ExamTurnParams = new[] { ParamKind.Singing };
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_EffectIndexOutOfRange_Throws()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            snapshot.Persistents[0].EffectIndex = 999;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_UnknownPersistentCardId_ThrowsArgumentException()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            snapshot.Persistents[0].CardId = int.MaxValue;
            Assert.Throws<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_EffectIndexNegative_Throws()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            snapshot.Persistents[0].EffectIndex = -1;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_EffectIndexNotPersistent_Throws()
        {
            var (catalog, cardId, snapshot) = PersistentSnapshotSource();
            var effects = catalog.Get(cardId).Base.Effects;
            var index = -1;
            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i].Op != EffectOp.AddPersistent)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                // 全部が持続効果のカードでは、範囲外の添字だけを確かめる
                index = effects.Count;
            }

            snapshot.Persistents[0].EffectIndex = index;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void ToState_EnhancedWithoutEnhancedFace_Throws()
        {
            var (catalog, _, snapshot) = PersistentSnapshotSource();
            var noEnhanced = catalog.CardIds.First(id => catalog.Get(id).Enhanced == null);
            snapshot.Persistents[0].CardId = noEnhanced;
            snapshot.Persistents[0].Enhanced = true;
            Assert.ThrowsAny<ArgumentException>(() => snapshot.ToState(catalog));
        }

        [Fact]
        public void LessonSnapshot_From_WithOtherCatalogInstance_Throws()
        {
            var (catalog, cardId) = FindPersistentCard(false);
            var state = PersistentState(catalog, cardId, false);

            Assert.Throws<InvalidOperationException>(() => LessonSnapshot.From(state, new TestCardCatalog()));
        }
    }
}

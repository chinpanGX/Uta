using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class ProduceReplayTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        private static ProduceStartInput Input(int scenario, int idol)
        {
            return new ProduceStartInput(scenario, idol, 1, new SupportCardInput[0]);
        }

        private static ProduceState Replay(ProduceCatalog catalog, ProduceStartInput input, ulong seed, IReadOnlyList<ProduceCommand> commands)
        {
            var engine = new ProduceEngine(catalog);
            var streams = new TestRandomStreamSource(seed);
            var state = engine.Start(input, streams);
            foreach (var command in commands)
            {
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
            }

            return state;
        }

        private static void AssertSameState(ProduceState expected, ProduceState actual)
        {
            Assert.Empty(ProduceStateComparer.Diff(expected, actual));
        }

        public static IEnumerable<object[]> Scenario1Cases()
        {
            foreach (var seed in new[] { 1, 2, 3 })
            {
                foreach (var idol in new[] { 1, 2 })
                {
                    yield return new object[] { seed, idol };
                }
            }
        }

        public static IEnumerable<object[]> Scenario2Cases()
        {
            foreach (var seed in new[] { 1, 2, 3 })
            {
                foreach (var idol in new[] { 1, 2, 3, 4 })
                {
                    yield return new object[] { seed, idol };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Scenario1Cases))]
        public void Replay_Scenario1_BotPlay_SameStateAsOriginal(int seed, int idol)
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource((ulong)seed);
            var input = Input(1, idol);
            var state = engine.Start(input, streams);
            var commands = ProduceBot.Run(engine, Catalog, state, streams, playCards: true, retryExam: false);

            var replayed = Replay(Catalog, input, (ulong)seed, commands);

            AssertSameState(state, replayed);
            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.Equal(commands.Count, state.Operations.Count);
            Assert.Equal(Enumerable.Range(0, commands.Count).ToArray(), state.Operations.Select(o => o.Number).ToArray());
        }

        [Theory]
        [MemberData(nameof(Scenario2Cases))]
        public void Replay_Scenario2_BotPlay_SameStateAsOriginal(int seed, int idol)
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource((ulong)seed);
            var input = Input(2, idol);
            var state = engine.Start(input, streams);
            var commands = ProduceBot.Run(engine, Catalog, state, streams, playCards: true, retryExam: false);

            var replayed = Replay(Catalog, input, (ulong)seed, commands);

            AssertSameState(state, replayed);
            Assert.Equal(ProducePhase.Finished, state.Phase);
            Assert.NotNull(state.Result);
        }

        [Fact]
        public void Replay_WithRetries_SameStateAsOriginal()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var input = Input(1, 1);
            var state = engine.Start(input, streams);
            var commands = ProduceBot.Run(engine, Catalog, state, streams, playCards: false, retryExam: true);

            Assert.Equal(67, commands.Count);
            var replayed = Replay(Catalog, input, 1, commands);

            AssertSameState(state, replayed);
            Assert.Equal(ProducePhase.Finished, replayed.Phase);
            Assert.Equal(ExamOutcome.FinalFailed, replayed.Result.Outcome);
        }

        [Fact]
        public void Replay_EveryStep_MatchesOriginalAfterEachCommand()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var input = Input(2, 1);
            var state = engine.Start(input, streams);
            var commands = new List<ProduceCommand>();
            var snapshots = new List<IReadOnlyList<string>>();
            while (state.Phase != ProducePhase.Finished)
            {
                var command = ProduceBot.Next(engine, Catalog, state, true, false);
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
                commands.Add(command);
                snapshots.Add(ProduceStateComparer.Describe(state));
            }

            var replayEngine = new ProduceEngine(Catalog);
            var replayStreams = new TestRandomStreamSource(1);
            var replayed = replayEngine.Start(input, replayStreams);
            for (var i = 0; i < commands.Count; i++)
            {
                Assert.Equal(ProduceApplyResult.Accepted, replayEngine.Apply(replayed, commands[i], replayStreams));
                Assert.Equal(snapshots[i], ProduceStateComparer.Describe(replayed));
            }
        }

        [Fact]
        public void Replay_RejectedCommandsInserted_FinalStateUnchanged()
        {
            var engine = new ProduceEngine(Catalog);
            var streams = new TestRandomStreamSource(1);
            var input = Input(1, 1);
            var state = engine.Start(input, streams);
            var commands = new List<ProduceCommand>();
            var rejected = new[] { ProduceCommand.OfferPick(0), ProduceCommand.UseCard(9999), ProduceCommand.ExamRetry() };
            var step = 0;
            while (state.Phase != ProducePhase.Finished)
            {
                var command = ProduceBot.Next(engine, Catalog, state, true, false);
                Assert.Equal(ProduceApplyResult.Accepted, engine.Apply(state, command, streams));
                commands.Add(command);
                if (step < rejected.Length && state.Phase != ProducePhase.Finished)
                {
                    var before = ProduceStateComparer.Describe(state);
                    var result = engine.Apply(state, rejected[step], streams);
                    Assert.NotEqual(ProduceApplyResult.Accepted, result);
                    Assert.Equal(before, ProduceStateComparer.Describe(state));
                }

                step++;
            }

            var replayed = Replay(Catalog, input, 1, commands);
            AssertSameState(replayed, state);
        }
    }
}

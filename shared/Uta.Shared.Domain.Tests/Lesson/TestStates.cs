using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Tests.Lesson
{
    /// <summary>テストの状態の作り方。</summary>
    public static class TestStates
    {
        public static LessonSetup NormalLesson { get; } = new LessonSetup(LessonKind.Normal, ParamKind.Singing, 6, 40, 80);

        /// <summary>カード ID の並びから、インスタンス ID を 1 から振ったデッキを作る。enhanced に挙げた ID は強化済み。</summary>
        public static List<CardInstance> Deck(IEnumerable<int> cardIds, int[] enhanced = null)
        {
            return cardIds.Select((cardId, index) => new CardInstance(index + 1, cardId, enhanced != null && enhanced.Contains(index + 1))).ToList();
        }

        /// <summary>プレイヤー操作の入力待ちの状態を、復元用のコンストラクタで作る。</summary>
        public static LessonState PlayerState(
            int[] cardIds,
            int[] hand,
            int[] drawPile,
            int hp,
            int usesLeft = 1,
            int paramGained = 0,
            int shield = 0,
            int[] discard = null,
            LessonSetup setup = null,
            int turn = 1,
            int maxHp = 30,
            IDictionary<StatusKind, int> statuses = null,
            int[] enhanced = null,
            PersistentEffect[] persistents = null)
        {
            var values = new int[StatusSet.KindCount];
            if (statuses != null)
            {
                foreach (var pair in statuses)
                {
                    values[(int)pair.Key] = pair.Value;
                }
            }

            return new LessonState(
                setup ?? NormalLesson,
                Deck(cardIds, enhanced),
                new Zones(drawPile, hand, discard ?? new int[0], new int[0]),
                new StatusSet(values, new bool[StatusSet.KindCount]),
                new ParamBonusEntry[0],
                new ScheduledEffect[0],
                persistents ?? new PersistentEffect[0],
                new int[0],
                turn,
                LessonPhase.Player,
                hp,
                maxHp,
                shield,
                usesLeft,
                paramGained,
                0);
        }

        /// <summary>保存して作り直したのと同じように、全部の値を写した別の状態を作る。</summary>
        public static LessonState Copy(LessonState s)
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

        public static Xoshiro256StarStar NewRandom()
        {
            return RandomStreams.Create(1, new RandomStreamKey(RandomStreamKind.Lesson, 1, 0));
        }
    }
}

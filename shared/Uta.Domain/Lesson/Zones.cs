using System;
using System.Collections.Generic;
using Uta.Domain.Rng;

namespace Uta.Domain.Lesson
{
    /// <summary>
    /// 山札・手札・捨札・除外（lesson-card.md 4章）。カードはインスタンス ID で持つ。山札は先頭が一番上。
    /// </summary>
    public sealed class Zones
    {
        private readonly List<int> drawPile;
        private readonly List<int> hand;
        private readonly List<int> discard;
        private readonly List<int> exiled;

        public Zones()
            : this(new int[0], new int[0], new int[0], new int[0])
        {
        }

        /// <summary>保存した状態から作り直す。</summary>
        public Zones(IEnumerable<int> drawPile, IEnumerable<int> hand, IEnumerable<int> discard, IEnumerable<int> exiled)
        {
            this.drawPile = new List<int>(drawPile ?? throw new ArgumentNullException(nameof(drawPile)));
            this.hand = new List<int>(hand ?? throw new ArgumentNullException(nameof(hand)));
            this.discard = new List<int>(discard ?? throw new ArgumentNullException(nameof(discard)));
            this.exiled = new List<int>(exiled ?? throw new ArgumentNullException(nameof(exiled)));
        }

        public IReadOnlyList<int> DrawPile => drawPile;
        public IReadOnlyList<int> Hand => hand;
        public IReadOnlyList<int> Discard => discard;
        public IReadOnlyList<int> Exiled => exiled;

        /// <summary>
        /// 山札から引く。山札が0枚になった瞬間に捨札をシャッフルして山札にする。山札も捨札も空なら引ける分だけ。
        /// </summary>
        /// <returns>実際に引いた枚数</returns>
        internal int Draw(int count, IRandom random)
        {
            var drawn = 0;
            for (var i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discard.Count == 0)
                    {
                        break;
                    }

                    ReshuffleDiscard(random);
                }

                hand.Add(drawPile[0]);
                drawPile.RemoveAt(0);
                drawn++;

                if (drawPile.Count == 0 && discard.Count > 0)
                {
                    // 山札を0枚にしたドローで手札に来たカードは、この再シャッフルに含まれない
                    ReshuffleDiscard(random);
                }
            }

            return drawn;
        }

        /// <summary>レッスン開始時の配置。山札は渡した順のまま置く（シャッフルは呼び出し側）。</summary>
        internal void SetUp(IEnumerable<int> initialHand, IEnumerable<int> initialDrawPile)
        {
            drawPile.Clear();
            hand.Clear();
            discard.Clear();
            exiled.Clear();
            hand.AddRange(initialHand);
            drawPile.AddRange(initialDrawPile);
        }

        internal bool RemoveFromHand(int instanceId)
        {
            return hand.Remove(instanceId);
        }

        internal void AddToDiscard(int instanceId)
        {
            discard.Add(instanceId);
        }

        internal void AddToExiled(int instanceId)
        {
            exiled.Add(instanceId);
        }

        /// <summary>手札をすべて、手札の並びの順に捨札へ。</summary>
        internal void DiscardHand()
        {
            discard.AddRange(hand);
            hand.Clear();
        }

        /// <summary>山札の position 番目（0 が一番上）に入れる。</summary>
        internal void InsertIntoDrawPile(int instanceId, int position)
        {
            drawPile.Insert(position, instanceId);
        }

        private void ReshuffleDiscard(IRandom random)
        {
            // 捨札は入った順に並べてからシャッフルする（produce.md 10.3）
            drawPile.AddRange(discard);
            discard.Clear();
            RandomOps.Shuffle(random, drawPile);
        }
    }
}

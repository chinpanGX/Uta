using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>終わった試験 1 回分の結果。</summary>
    public sealed class ExamReport
    {
        public ExamReport(ExamKind kind, int attempt, int score, int rank, IReadOnlyList<int> npcScores, bool passed)
        {
            if (npcScores == null)
            {
                throw new ArgumentNullException(nameof(npcScores));
            }

            var copy = new int[npcScores.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = npcScores[i];
            }

            Kind = kind;
            Attempt = attempt;
            Score = score;
            Rank = rank;
            NpcScores = copy;
            Passed = passed;
        }

        public ExamKind Kind { get; }
        public int Attempt { get; }
        public int Score { get; }
        public int Rank { get; }
        public IReadOnlyList<int> NpcScores { get; }
        public bool Passed { get; }
    }

    /// <summary>試験の開始直前の「育成」の区分（produce.md 5.1.1）。</summary>
    public sealed class ExamBackup
    {
        public ExamBackup(IReadOnlyList<int> parameters, int hp, IReadOnlyList<CardInstance> deck, int nextInstanceId)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Count != ProduceRules.ParamCount)
            {
                throw new ArgumentException("パラメータは " + ProduceRules.ParamCount + " 個です。", nameof(parameters));
            }

            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            var paramCopy = new int[parameters.Count];
            for (var i = 0; i < paramCopy.Length; i++)
            {
                paramCopy[i] = parameters[i];
            }

            var deckCopy = new CardInstance[deck.Count];
            for (var i = 0; i < deckCopy.Length; i++)
            {
                deckCopy[i] = deck[i];
            }

            Parameters = paramCopy;
            Hp = hp;
            Deck = deckCopy;
            NextInstanceId = nextInstanceId;
        }

        /// <summary>長さ 3（ProduceParams の添字）</summary>
        public IReadOnlyList<int> Parameters { get; }

        public int Hp { get; }
        public IReadOnlyList<CardInstance> Deck { get; }
        public int NextInstanceId { get; }
    }

    /// <summary>育成の結果。</summary>
    public sealed class ProduceResult
    {
        public ProduceResult(ExamOutcome outcome, int evaluationValue, ProduceRank rank, int finalScore, int finalRank)
        {
            Outcome = outcome;
            EvaluationValue = evaluationValue;
            Rank = rank;
            FinalScore = finalScore;
            FinalRank = finalRank;
        }

        public ExamOutcome Outcome { get; }
        public int EvaluationValue { get; }
        public ProduceRank Rank { get; }

        /// <summary>最終試験を受けていなければ 0</summary>
        public int FinalScore { get; }

        /// <summary>最終試験を受けていなければ 0</summary>
        public int FinalRank { get; }
    }

    /// <summary>
    /// 育成の状態。ProduceCatalog・ProduceEngine・定義の型への参照を持たない
    /// （値と配列と LessonState だけ。順 6 が平らに保存できるようにするため）。
    /// </summary>
    public sealed class ProduceState
    {
        private readonly int[] parameters;
        private readonly int[] bonusPerThousand;
        private readonly List<CardInstance> deck;
        private readonly List<int> classOptionParams = new List<int>();
        private readonly List<int> offerCards = new List<int>();
        private readonly List<OperationRecord> operations = new List<OperationRecord>();

        internal ProduceState(
            ProduceStartInput input,
            int maxHp,
            int[] parameters,
            int[] bonusPerThousand,
            IReadOnlyList<CardInstance> deck,
            int examRetryLeft)
        {
            Input = input;
            MaxHp = maxHp;
            Hp = maxHp;
            this.parameters = (int[])parameters.Clone();
            this.bonusPerThousand = (int[])bonusPerThousand.Clone();
            this.deck = new List<CardInstance>(deck);
            NextInstanceId = deck.Count + 1;
            ExamRetryLeft = examRetryLeft;
            Week = 1;
            Attempt = 0;
            NextOperationNumber = 0;
        }

        public ProduceStartInput Input { get; }
        public ProducePhase Phase { get; internal set; }

        /// <summary>1 から</summary>
        public int Week { get; internal set; }

        /// <summary>今の試験の挑戦回数（0 始まり）。Exam / ExamResult の間だけ 0 以上の意味を持ち、試験の外では 0。直近の試験の挑戦回数は LastExam.Attempt</summary>
        public int Attempt { get; internal set; }

        public int NextOperationNumber { get; internal set; }
        public int NextInstanceId { get; internal set; }
        public int Hp { get; internal set; }
        public int MaxHp { get; }
        public int ExamRetryLeft { get; internal set; }

        public int GetParam(ParamKind kind)
        {
            return parameters[ProduceParams.ToIndex(kind)];
        }

        /// <summary>開始時に決めた合算値</summary>
        public int GetBonusPerThousand(ParamKind kind)
        {
            return bonusPerThousand[ProduceParams.ToIndex(kind)];
        }

        public IReadOnlyList<CardInstance> Deck => deck;

        /// <summary>授業の選択肢ごとの ParamKind の数値。授業のある週の WeekSelect だけ値あり、ほかは空</summary>
        public IReadOnlyList<int> ClassOptionParams => classOptionParams;

        /// <summary>CardOffer のときだけ値あり</summary>
        public IReadOnlyList<int> OfferCards => offerCards;

        /// <summary>Lesson / Exam のときだけ。ほかは null</summary>
        public LessonState Lesson { get; internal set; }

        /// <summary>Exam / ExamResult のときだけ。ほかは null</summary>
        public ExamBackup ExamBackup { get; internal set; }

        /// <summary>直近に終わった試験。まだなければ null</summary>
        public ExamReport LastExam { get; internal set; }

        /// <summary>中間試験を終えたあとの結果（最後の挑戦）。まだなければ null</summary>
        public ExamReport MidExam { get; internal set; }

        /// <summary>Finished のときだけ</summary>
        public ProduceResult Result { get; internal set; }

        public IReadOnlyList<OperationRecord> Operations => operations;

        internal void SetParam(ParamKind kind, int value)
        {
            parameters[ProduceParams.ToIndex(kind)] = value;
        }

        internal int[] CopyParams()
        {
            return (int[])parameters.Clone();
        }

        internal void AddCard(CardInstance card)
        {
            deck.Add(card);
        }

        internal void ReplaceDeck(IReadOnlyList<CardInstance> cards)
        {
            deck.Clear();
            for (var i = 0; i < cards.Count; i++)
            {
                deck.Add(cards[i]);
            }
        }

        internal void SetClassOptionParams(IReadOnlyList<int> values)
        {
            classOptionParams.Clear();
            for (var i = 0; i < values.Count; i++)
            {
                classOptionParams.Add(values[i]);
            }
        }

        internal void ClearClassOptionParams()
        {
            classOptionParams.Clear();
        }

        internal void SetOfferCards(IReadOnlyList<int> values)
        {
            offerCards.Clear();
            for (var i = 0; i < values.Count; i++)
            {
                offerCards.Add(values[i]);
            }
        }

        internal void ClearOfferCards()
        {
            offerCards.Clear();
        }

        internal void AddOperation(OperationRecord record)
        {
            operations.Add(record);
        }
    }
}

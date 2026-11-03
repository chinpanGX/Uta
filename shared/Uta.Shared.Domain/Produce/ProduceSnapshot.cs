using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>サポートカード 1 枚の保存用の写し。</summary>
    public sealed class SupportCardSnapshot
    {
        public int CardId { get; set; }
        public int Level { get; set; }
        public int LimitBreak { get; set; }
    }

    /// <summary>ExamReport の保存用の写し。</summary>
    public sealed class ExamReportSnapshot
    {
        public ExamKind Kind { get; set; }
        public int Attempt { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        public int[] NpcScores { get; set; } = new int[0];
        public bool Passed { get; set; }
    }

    /// <summary>ExamBackup の保存用の写し。</summary>
    public sealed class ExamBackupSnapshot
    {
        public int[] Parameters { get; set; } = new int[0];
        public int Hp { get; set; }
        public CardInstanceSnapshot[] Deck { get; set; } = new CardInstanceSnapshot[0];
        public int NextInstanceId { get; set; }
    }

    /// <summary>ProduceResult の保存用の写し。</summary>
    public sealed class ProduceResultSnapshot
    {
        public ExamOutcome Outcome { get; set; }
        public int EvaluationValue { get; set; }
        public ProduceRank Rank { get; set; }
        public int FinalScore { get; set; }
        public int FinalRank { get; set; }
    }

    /// <summary>操作の記録 1 件の保存用の写し。</summary>
    public sealed class OperationSnapshot
    {
        public int Number { get; set; }
        public OperationKind Kind { get; set; }
        public int Arg1 { get; set; }
        public int Arg2 { get; set; }
        public int Arg3 { get; set; }
        public int Week { get; set; }
        public int Attempt { get; set; }
        public int Turn { get; set; }
    }

    /// <summary>
    /// ProduceState の保存用の平らな写し（produce-domain.md 6.1）。値と配列だけを持つ。
    /// </summary>
    public sealed class ProduceSnapshot
    {
        public int ScenarioId { get; set; }
        public int IdolId { get; set; }
        public int PlayerLevel { get; set; }
        public SupportCardSnapshot[] SupportCards { get; set; } = new SupportCardSnapshot[0];
        public ProducePhase Phase { get; set; }
        public int Week { get; set; }
        public int Attempt { get; set; }
        public int NextOperationNumber { get; set; }
        public int NextInstanceId { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int ExamRetryLeft { get; set; }
        public int[] Parameters { get; set; } = new int[0];
        public int[] BonusPerThousand { get; set; } = new int[0];
        public CardInstanceSnapshot[] Deck { get; set; } = new CardInstanceSnapshot[0];
        public int[] ClassOptionParams { get; set; } = new int[0];
        public int[] OfferCards { get; set; } = new int[0];
        public LessonSnapshot Lesson { get; set; }
        public ExamBackupSnapshot ExamBackup { get; set; }
        public ExamReportSnapshot LastExam { get; set; }
        public ExamReportSnapshot MidExam { get; set; }
        public ProduceResultSnapshot Result { get; set; }
        public OperationSnapshot[] Operations { get; set; } = new OperationSnapshot[0];

        /// <summary>
        /// 状態から写しを作る。cards は状態を動かしたエンジンの ProduceCatalog.Cards と同じインスタンスであること。
        /// </summary>
        public static ProduceSnapshot From(ProduceState state, ICardCatalog cards)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var snapshot = new ProduceSnapshot
            {
                ScenarioId = state.Input.ScenarioId,
                IdolId = state.Input.IdolId,
                PlayerLevel = state.Input.PlayerLevel,
                Phase = state.Phase,
                Week = state.Week,
                Attempt = state.Attempt,
                NextOperationNumber = state.NextOperationNumber,
                NextInstanceId = state.NextInstanceId,
                Hp = state.Hp,
                MaxHp = state.MaxHp,
                ExamRetryLeft = state.ExamRetryLeft,
                Parameters = state.CopyParams(),
                Deck = LessonSnapshot.CopyCards(state.Deck),
                ClassOptionParams = LessonSnapshot.CopyInts(state.ClassOptionParams),
                OfferCards = LessonSnapshot.CopyInts(state.OfferCards),
            };

            snapshot.SupportCards = new SupportCardSnapshot[state.Input.SupportCards.Count];
            for (var i = 0; i < snapshot.SupportCards.Length; i++)
            {
                var support = state.Input.SupportCards[i];
                snapshot.SupportCards[i] = new SupportCardSnapshot
                {
                    CardId = support.CardId,
                    Level = support.Level,
                    LimitBreak = support.LimitBreak,
                };
            }

            snapshot.BonusPerThousand = new int[ProduceRules.ParamCount];
            for (var i = 0; i < snapshot.BonusPerThousand.Length; i++)
            {
                snapshot.BonusPerThousand[i] = state.GetBonusPerThousand(ProduceParams.FromIndex(i));
            }

            snapshot.Lesson = state.Lesson == null ? null : LessonSnapshot.From(state.Lesson, cards);

            if (state.ExamBackup != null)
            {
                snapshot.ExamBackup = new ExamBackupSnapshot
                {
                    Parameters = LessonSnapshot.CopyInts(state.ExamBackup.Parameters),
                    Hp = state.ExamBackup.Hp,
                    Deck = LessonSnapshot.CopyCards(state.ExamBackup.Deck),
                    NextInstanceId = state.ExamBackup.NextInstanceId,
                };
            }

            snapshot.LastExam = CopyReport(state.LastExam);
            snapshot.MidExam = CopyReport(state.MidExam);

            if (state.Result != null)
            {
                snapshot.Result = new ProduceResultSnapshot
                {
                    Outcome = state.Result.Outcome,
                    EvaluationValue = state.Result.EvaluationValue,
                    Rank = state.Result.Rank,
                    FinalScore = state.Result.FinalScore,
                    FinalRank = state.Result.FinalRank,
                };
            }

            snapshot.Operations = new OperationSnapshot[state.Operations.Count];
            for (var i = 0; i < snapshot.Operations.Length; i++)
            {
                var op = state.Operations[i];
                snapshot.Operations[i] = new OperationSnapshot
                {
                    Number = op.Number,
                    Kind = op.Command.Kind,
                    Arg1 = op.Command.Arg1,
                    Arg2 = op.Command.Arg2,
                    Arg3 = op.Command.Arg3,
                    Week = op.Week,
                    Attempt = op.Attempt,
                    Turn = op.Turn,
                };
            }

            return snapshot;
        }

        /// <summary>写しから状態を作り直す。復元できない値は ArgumentException。</summary>
        public ProduceState ToState(ProduceCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            LessonSnapshot.RequireNotNull(SupportCards, nameof(SupportCards));
            LessonSnapshot.RequireNotNull(Parameters, nameof(Parameters));
            LessonSnapshot.RequireNotNull(BonusPerThousand, nameof(BonusPerThousand));
            LessonSnapshot.RequireNotNull(Deck, nameof(Deck));
            LessonSnapshot.RequireNotNull(ClassOptionParams, nameof(ClassOptionParams));
            LessonSnapshot.RequireNotNull(OfferCards, nameof(OfferCards));
            LessonSnapshot.RequireNotNull(Operations, nameof(Operations));
            LessonSnapshot.RequireNoNullElement(SupportCards, nameof(SupportCards));
            LessonSnapshot.RequireNoNullElement(Deck, nameof(Deck));
            LessonSnapshot.RequireNoNullElement(Operations, nameof(Operations));
            if (ExamBackup != null)
            {
                LessonSnapshot.RequireNotNull(ExamBackup.Deck, nameof(ExamBackup) + "." + nameof(ExamBackup.Deck));
                LessonSnapshot.RequireNoNullElement(ExamBackup.Deck, nameof(ExamBackup) + "." + nameof(ExamBackup.Deck));
            }

            if (Parameters.Length != ProduceRules.ParamCount)
            {
                throw new ArgumentException(nameof(Parameters) + ": 長さが " + ProduceRules.ParamCount + " ではありません。");
            }

            if (BonusPerThousand.Length != ProduceRules.ParamCount)
            {
                throw new ArgumentException(nameof(BonusPerThousand) + ": 長さが " + ProduceRules.ParamCount + " ではありません。");
            }

            var supportInputs = new SupportCardInput[SupportCards.Length];
            for (var i = 0; i < supportInputs.Length; i++)
            {
                supportInputs[i] = new SupportCardInput(SupportCards[i].CardId, SupportCards[i].Level, SupportCards[i].LimitBreak);
            }

            var input = new ProduceStartInput(ScenarioId, IdolId, PlayerLevel, supportInputs);
            var state = new ProduceState(input, MaxHp, Parameters, BonusPerThousand, LessonSnapshot.ToInstances(Deck), ExamRetryLeft);
            state.Phase = Phase;
            state.Week = Week;
            state.Attempt = Attempt;
            state.NextOperationNumber = NextOperationNumber;
            state.NextInstanceId = NextInstanceId;
            state.Hp = Hp;
            state.SetClassOptionParams(ClassOptionParams);
            state.SetOfferCards(OfferCards);

            if (Lesson != null)
            {
                state.Lesson = Lesson.ToState(catalog.Cards);
            }

            if (ExamBackup != null)
            {
                state.ExamBackup = new ExamBackup(ExamBackup.Parameters, ExamBackup.Hp, LessonSnapshot.ToInstances(ExamBackup.Deck), ExamBackup.NextInstanceId);
            }

            state.LastExam = ToReport(LastExam);
            state.MidExam = ToReport(MidExam);

            if (Result != null)
            {
                state.Result = new ProduceResult(Result.Outcome, Result.EvaluationValue, Result.Rank, Result.FinalScore, Result.FinalRank);
            }

            for (var i = 0; i < Operations.Length; i++)
            {
                var op = Operations[i];
                state.AddOperation(new OperationRecord(op.Number, new ProduceCommand(op.Kind, op.Arg1, op.Arg2, op.Arg3), op.Week, op.Attempt, op.Turn));
            }

            return state;
        }

        private static ExamReportSnapshot CopyReport(ExamReport report)
        {
            if (report == null)
            {
                return null;
            }

            return new ExamReportSnapshot
            {
                Kind = report.Kind,
                Attempt = report.Attempt,
                Score = report.Score,
                Rank = report.Rank,
                NpcScores = LessonSnapshot.CopyInts(report.NpcScores),
                Passed = report.Passed,
            };
        }

        private static ExamReport ToReport(ExamReportSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new ExamReport(snapshot.Kind, snapshot.Attempt, snapshot.Score, snapshot.Rank, snapshot.NpcScores, snapshot.Passed);
        }
    }
}

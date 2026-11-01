using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>育成の操作 1 件（種類と引数 3 つ）。produce-domain.md 5章。</summary>
    public readonly struct ProduceCommand
    {
        public ProduceCommand(OperationKind kind, int arg1, int arg2, int arg3)
        {
            Kind = kind;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
        }

        public OperationKind Kind { get; }
        public int Arg1 { get; }
        public int Arg2 { get; }
        public int Arg3 { get; }

        /// <summary>レッスンを選ぶ。引数 1 = 行動の枠番号、引数 3 = パラメータ。</summary>
        public static ProduceCommand ChooseLesson(int slotIndex, ParamKind param)
        {
            return new ProduceCommand(OperationKind.ChooseWeekAction, slotIndex, -1, (int)param);
        }

        /// <summary>授業を選ぶ。引数 2 = 選択肢の番号。</summary>
        public static ProduceCommand ChooseClass(int slotIndex, int optionIndex)
        {
            return new ProduceCommand(OperationKind.ChooseWeekAction, slotIndex, optionIndex, -1);
        }

        /// <summary>支給・休むを選ぶ。</summary>
        public static ProduceCommand ChooseOther(int slotIndex)
        {
            return new ProduceCommand(OperationKind.ChooseWeekAction, slotIndex, -1, -1);
        }

        public static ProduceCommand OfferPick(int offerIndex)
        {
            return new ProduceCommand(OperationKind.OfferPick, offerIndex, 0, 0);
        }

        public static ProduceCommand OfferReroll()
        {
            return new ProduceCommand(OperationKind.OfferReroll, 0, 0, 0);
        }

        public static ProduceCommand OfferRemove(int offerIndex)
        {
            return new ProduceCommand(OperationKind.OfferRemove, offerIndex, 0, 0);
        }

        public static ProduceCommand OfferDecline()
        {
            return new ProduceCommand(OperationKind.OfferDecline, 0, 0, 0);
        }

        public static ProduceCommand ExamRetry()
        {
            return new ProduceCommand(OperationKind.ExamRetry, 0, 0, 0);
        }

        public static ProduceCommand ExamNoRetry()
        {
            return new ProduceCommand(OperationKind.ExamNoRetry, 0, 0, 0);
        }

        public static ProduceCommand UseCard(int instanceId)
        {
            return new ProduceCommand(OperationKind.UseCard, instanceId, 0, 0);
        }

        public static ProduceCommand UseDrink(int slotIndex)
        {
            return new ProduceCommand(OperationKind.UseDrink, slotIndex, 0, 0);
        }

        public static ProduceCommand Skip()
        {
            return new ProduceCommand(OperationKind.Skip, 0, 0, 0);
        }
    }

    /// <summary>受け付けた操作の記録。コマンドに操作番号・週・挑戦回数・ターンを付けたもの。</summary>
    public sealed class OperationRecord
    {
        public OperationRecord(int number, ProduceCommand command, int week, int attempt, int turn)
        {
            Number = number;
            Command = command;
            Week = week;
            Attempt = attempt;
            Turn = turn;
        }

        /// <summary>0 から連番</summary>
        public int Number { get; }

        public ProduceCommand Command { get; }

        /// <summary>受け付ける前の週番号</summary>
        public int Week { get; }

        /// <summary>受け付ける前の ProduceState.Attempt（試験の外は 0）</summary>
        public int Attempt { get; }

        /// <summary>受け付ける前のレッスン・試験のターン番号。レッスン・試験の外は 0</summary>
        public int Turn { get; }
    }
}

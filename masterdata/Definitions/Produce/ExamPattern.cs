using MasterMemory;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("examPattern"), MessagePackObject(true)]
    public class ExamPattern : IValidatable<ExamPattern>
    {
        [PrimaryKey]
        public int ExamPatternId { get; init; }

        public int FirstPercent { get; init; }

        public int SecondPercent { get; init; }

        public int ThirdPercent { get; init; }

        public int MidFirstTurns { get; init; }

        public int MidSecondTurns { get; init; }

        public int MidThirdTurns { get; init; }

        public int FinalSenseFirstTurns { get; init; }

        public int FinalSenseSecondTurns { get; init; }

        public int FinalSenseThirdTurns { get; init; }

        public int FinalLogicFirstTurns { get; init; }

        public int FinalLogicSecondTurns { get; init; }

        public int FinalLogicThirdTurns { get; init; }

        public int FirstTurnSecondRatePerTenThousand { get; init; }

        void IValidatable<ExamPattern>.Validate(IValidator<ExamPattern> validator)
        {
            validator.Validate(x => x.FirstPercent + x.SecondPercent + x.ThirdPercent == 100, "examPattern: Percent の合計が 100 でない");
            validator.Validate(x => x.FirstPercent >= 0 && x.SecondPercent >= 0 && x.ThirdPercent >= 0, "examPattern: Percent が負");
            validator.Validate(
                x => x.FirstPercent < 45 || x.FirstTurnSecondRatePerTenThousand == 0,
                "examPattern: FirstPercent が 45 以上なのに FirstTurnSecondRatePerTenThousand が 0 でない");
            validator.Validate(x => x.MidFirstTurns + x.MidSecondTurns + x.MidThirdTurns == 9, "examPattern: Mid のターン数の合計が 9 でない");
            validator.Validate(
                x => x.FinalSenseFirstTurns + x.FinalSenseSecondTurns + x.FinalSenseThirdTurns == 12,
                "examPattern: FinalSense のターン数の合計が 12 でない");
            validator.Validate(
                x => x.FinalLogicFirstTurns + x.FinalLogicSecondTurns + x.FinalLogicThirdTurns == 11,
                "examPattern: FinalLogic のターン数の合計が 11 でない");
            validator.Validate(
                x => x.MidFirstTurns >= 1 && x.MidSecondTurns >= 1 && x.MidThirdTurns >= 1
                    && x.FinalSenseFirstTurns >= 1 && x.FinalSenseSecondTurns >= 1 && x.FinalSenseThirdTurns >= 1
                    && x.FinalLogicFirstTurns >= 1 && x.FinalLogicSecondTurns >= 1 && x.FinalLogicThirdTurns >= 1,
                "examPattern: ターン数が 1 未満");
            validator.Validate(
                x => x.FirstTurnSecondRatePerTenThousand >= 0 && x.FirstTurnSecondRatePerTenThousand <= 10000,
                "examPattern: FirstTurnSecondRatePerTenThousand が 0〜10000 の外");
        }
    }
}

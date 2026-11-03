using MessagePack;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Contracts.Produce
{
    [MessagePackObject]
    public sealed class ExamTurnDto
    {
        [SerializationConstructor]
        public ExamTurnDto(ParamKind param, int scoreBonusPercent)
        {
            Param = param;
            ScoreBonusPercent = scoreBonusPercent;
        }

        [Key(0)]
        public ParamKind Param { get; }

        [Key(1)]
        public int ScoreBonusPercent { get; }
    }
}

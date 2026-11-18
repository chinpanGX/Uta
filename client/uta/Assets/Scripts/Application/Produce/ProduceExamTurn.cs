using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Application.Produce
{
    public sealed class ProduceExamTurn
    {
        public ProduceExamTurn(ParamKind param, int scoreBonusPercent)
        {
            Param = param;
            ScoreBonusPercent = scoreBonusPercent;
        }

        public ParamKind Param { get; }
        public int ScoreBonusPercent { get; }
    }
}

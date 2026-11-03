using MasterMemory;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("evaluationScoreBand"), MessagePackObject(true)]
    public class EvaluationScoreBand : IValidatable<EvaluationScoreBand>
    {
        [PrimaryKey]
        public int Order { get; init; }

        public int MinScore { get; init; }

        public int ScalePerThousand { get; init; }

        public int Fixed { get; init; }

        void IValidatable<EvaluationScoreBand>.Validate(IValidator<EvaluationScoreBand> validator)
        {
            validator.Validate(x => x.Order >= 1, "evaluationScoreBand: Order が 1 未満");
            validator.Validate(x => x.MinScore >= 0, "evaluationScoreBand: MinScore が負");
            validator.Validate(x => x.ScalePerThousand >= 0, "evaluationScoreBand: ScalePerThousand が負");
            validator.Validate(x => x.Fixed >= 0, "evaluationScoreBand: Fixed が負");
        }
    }
}

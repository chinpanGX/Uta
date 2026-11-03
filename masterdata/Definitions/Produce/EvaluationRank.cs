using MasterMemory;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("evaluationRank"), MessagePackObject(true)]
    public class EvaluationRank : IValidatable<EvaluationRank>
    {
        [PrimaryKey]
        public ProduceRank Rank { get; init; }

        public int MinValue { get; init; }

        void IValidatable<EvaluationRank>.Validate(IValidator<EvaluationRank> validator)
        {
            validator.Validate(x => x.Rank != ProduceRank.Nothing, "evaluationRank: Rank が Nothing");
            validator.Validate(x => x.MinValue >= 0, "evaluationRank: MinValue が負");
        }
    }
}

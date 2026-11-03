using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("weekActionSlot"), MessagePackObject(true)]
    public class WeekActionSlot : IValidatable<WeekActionSlot>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Scenario), nameof(Scenario.ScenarioId))]
        public int ScenarioId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public int Week { get; init; }

        [PrimaryKey(keyOrder: 2)]
        public int Order { get; init; }

        public ActionKind Kind { get; init; }

        void IValidatable<WeekActionSlot>.Validate(IValidator<WeekActionSlot> validator)
        {
            validator.Validate(x => x.Kind != ActionKind.Nothing, "weekActionSlot: Kind が Nothing");
            validator.Validate(x => x.Order >= 1, "weekActionSlot: Order が 1 未満");
        }
    }
}

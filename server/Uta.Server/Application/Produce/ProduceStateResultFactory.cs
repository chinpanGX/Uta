using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public sealed class ProduceStateResultFactory(ProduceEngine engine, ProduceCatalog catalog)
{
    public ProduceStateResult Create(ProduceState state, ExamReport? examResult, ProduceEndResult? end)
    {
        var weekSelect = state.Phase == ProducePhase.WeekSelect;
        var npcScores = state.Phase == ProducePhase.Exam
            ? ProduceFormulas.NpcScores(catalog.GetScenario(state.Input.ScenarioId).GetWeek(state.Week).RivalBaseScore)
            : [];
        var report = examResult ?? (state.Phase == ProducePhase.ExamResult ? state.LastExam : null);
        return new ProduceStateResult(
            state,
            weekSelect ? engine.GetWeekChoices(state) : [],
            weekSelect ? engine.GetClassOptions(state) : [],
            npcScores,
            report,
            end);
    }
}

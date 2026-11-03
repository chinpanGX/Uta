using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public sealed class ProduceViewBuilder(ProduceEngine engine, ProduceCatalog catalog)
{
    public ProduceView Build(ProduceState state, ExamReport? examResult, ProduceEndSummary? end)
    {
        var weekSelect = state.Phase == ProducePhase.WeekSelect;
        var npcScores = state.Phase == ProducePhase.Exam
            ? ProduceFormulas.NpcScores(catalog.GetScenario(state.Input.ScenarioId).GetWeek(state.Week).RivalBaseScore)
            : [];
        var report = examResult ?? (state.Phase == ProducePhase.ExamResult ? state.LastExam : null);
        return new ProduceView(
            state,
            weekSelect ? engine.GetWeekChoices(state) : [],
            weekSelect ? engine.GetClassOptions(state) : [],
            npcScores,
            report,
            end);
    }
}

using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;

namespace Uta.Server.Application.Produce;

public static class ProduceCommandNormalizer
{
    /// <summary>ChooseWeekAction の使わない引数を正規形にする（produce-domain.md 5章）。それ以外・範囲外の枠・WeekSelect でないときは raw のまま（Apply が拒否する）。</summary>
    public static ProduceCommand Normalize(ProduceEngine engine, ProduceState state, ProduceCommand raw)
    {
        if (raw.Kind != OperationKind.ChooseWeekAction || state.Phase != ProducePhase.WeekSelect)
        {
            return raw;
        }

        var choices = engine.GetWeekChoices(state);
        var slot = raw.Arg1;
        if (slot < 0 || slot >= choices.Count)
        {
            return raw;
        }

        return choices[slot].Kind switch
        {
            WeekChoiceKind.Lesson => ProduceCommand.ChooseLesson(slot, (ParamKind)raw.Arg3),
            WeekChoiceKind.Class => ProduceCommand.ChooseClass(slot, raw.Arg2),
            _ => ProduceCommand.ChooseOther(slot),
        };
    }
}

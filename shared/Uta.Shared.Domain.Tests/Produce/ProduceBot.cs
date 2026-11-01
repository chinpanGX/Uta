using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Tests.Produce
{
    /// <summary>テスト用の自動プレイ。局面ごとに次のコマンドを決めて Apply する。</summary>
    public static class ProduceBot
    {
        public static ProduceCommand Next(ProduceEngine engine, ProduceCatalog catalog, ProduceState state, bool playCards = true, bool retryExam = false)
        {
            switch (state.Phase)
            {
                case ProducePhase.WeekSelect:
                {
                    var choices = engine.GetWeekChoices(state);
                    switch (choices[0].Kind)
                    {
                        case WeekChoiceKind.Lesson:
                            return ProduceCommand.ChooseLesson(0, catalog.GetIdol(state.Input.IdolId).FirstParam);
                        case WeekChoiceKind.Class:
                        {
                            var options = engine.GetClassOptions(state);
                            for (var i = 0; i < options.Count; i++)
                            {
                                if (options[i].Selectable)
                                {
                                    return ProduceCommand.ChooseClass(0, options[i].Index);
                                }
                            }

                            throw new InvalidOperationException("選べる授業の選択肢がありません。");
                        }

                        default:
                            return ProduceCommand.ChooseOther(0);
                    }
                }

                case ProducePhase.CardOffer:
                    return ProduceCommand.OfferPick(0);
                case ProducePhase.Lesson:
                case ProducePhase.Exam:
                {
                    if (playCards)
                    {
                        foreach (var instanceId in state.Lesson.Zones.Hand)
                        {
                            if (engine.CanUse(state, instanceId) == UseCheckResult.Ok)
                            {
                                return ProduceCommand.UseCard(instanceId);
                            }
                        }
                    }

                    return ProduceCommand.Skip();
                }

                case ProducePhase.ExamResult:
                    return retryExam ? ProduceCommand.ExamRetry() : ProduceCommand.ExamNoRetry();
                default:
                    throw new InvalidOperationException("次のコマンドはありません: " + state.Phase);
            }
        }

        public static List<ProduceCommand> Run(
            ProduceEngine engine,
            ProduceCatalog catalog,
            ProduceState state,
            IRandomStreamSource streams,
            bool playCards = true,
            bool retryExam = false,
            int maxOperations = 3000)
        {
            return RunUntil(engine, catalog, state, streams, _ => false, playCards, retryExam, maxOperations);
        }

        public static List<ProduceCommand> RunUntil(
            ProduceEngine engine,
            ProduceCatalog catalog,
            ProduceState state,
            IRandomStreamSource streams,
            Func<ProduceState, bool> stop,
            bool playCards = true,
            bool retryExam = false,
            int maxOperations = 3000)
        {
            var used = new List<ProduceCommand>();
            while (state.Phase != ProducePhase.Finished && !stop(state))
            {
                if (used.Count >= maxOperations)
                {
                    throw new InvalidOperationException("操作の回数が上限を超えました。");
                }

                var command = Next(engine, catalog, state, playCards, retryExam);
                var result = engine.Apply(state, command, streams);
                if (result != ProduceApplyResult.Accepted)
                {
                    throw new InvalidOperationException("操作が受け付けられませんでした: " + result);
                }

                used.Add(command);
            }

            return used;
        }
    }
}

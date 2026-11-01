using System;
using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Rng;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>週の選択の枠。</summary>
    public readonly struct WeekChoice
    {
        public WeekChoice(int slotIndex, WeekChoiceKind kind, bool selectable)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            Selectable = selectable;
        }

        public int SlotIndex { get; }
        public WeekChoiceKind Kind { get; }
        public bool Selectable { get; }
    }

    /// <summary>授業の選択肢の表示用の値。</summary>
    public readonly struct ClassOptionView
    {
        public ClassOptionView(int index, int hpCost, ParamKind param, int paramGain, bool givesCard, bool selectable)
        {
            Index = index;
            HpCost = hpCost;
            Param = param;
            ParamGain = paramGain;
            GivesCard = givesCard;
            Selectable = selectable;
        }

        public int Index { get; }
        public int HpCost { get; }
        public ParamKind Param { get; }
        public int ParamGain { get; }
        public bool GivesCard { get; }
        public bool Selectable { get; }
    }

    /// <summary>育成の進行（produce.md・produce-domain.md）。乱数は IRandomStreamSource から引く。</summary>
    public sealed class ProduceEngine
    {
        private readonly ProduceCatalog catalog;
        private readonly LessonEngine lessonEngine;

        public ProduceEngine(ProduceCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            lessonEngine = new LessonEngine(catalog.Cards);
        }

        /// <summary>プロデュースを始め、週 1 の入力待ちまで進める。</summary>
        public ProduceState Start(ProduceStartInput input, IRandomStreamSource streams)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (streams == null)
            {
                throw new ArgumentNullException(nameof(streams));
            }

            var scenario = catalog.GetScenario(input.ScenarioId);
            var idol = catalog.GetIdol(input.IdolId);
            var supports = new SupportCardDefinition[input.SupportCards.Count];
            var supportIds = new int[supports.Length];
            for (var i = 0; i < supports.Length; i++)
            {
                supports[i] = catalog.GetSupportCard(input.SupportCards[i].CardId);
                supportIds[i] = supports[i].SupportCardId;
            }

            for (var i = 0; i < supports.Length; i++)
            {
                var level = input.SupportCards[i].Level;
                var maxLevel = catalog.GetMaxLevel(supports[i].Rarity, input.SupportCards[i].LimitBreak);
                if (level < 1 || level > maxLevel)
                {
                    throw new ArgumentException("サポートカードのレベルが範囲外です。", nameof(input));
                }
            }

            if (FormationValidator.Validate(catalog, idol.IdolId, supportIds) != FormationCheck.Ok)
            {
                throw new ArgumentException("編成が正しくありません。", nameof(input));
            }

            var maxHp = idol.MaxHp;
            var bonus = new int[ProduceRules.ParamCount];
            for (var p = 0; p < bonus.Length; p++)
            {
                bonus[p] = idol.GetBonusPerThousand(ProduceParams.FromIndex(p));
            }

            for (var i = 0; i < supports.Length; i++)
            {
                var card = supports[i];
                var level = input.SupportCards[i].Level;
                for (var a = 0; a < card.Abilities.Count; a++)
                {
                    var ability = card.Abilities[a];
                    var value = ProduceFormulas.AbilityValue(ability.ValueAtLv1, ability.ValueAtMax, level, card.MaxLevelAtFullLimitBreak);
                    if (ability.Kind == SupportAbilityKind.MaxHp)
                    {
                        maxHp += value;
                    }
                    else if (ability.Kind == SupportAbilityKind.ParamBonus)
                    {
                        bonus[ProduceParams.ToIndex(ability.Param)] += value;
                    }
                }
            }

            var parameters = new int[ProduceRules.ParamCount];
            for (var p = 0; p < parameters.Length; p++)
            {
                parameters[p] = ProduceFormulas.AddParam(0, idol.GetInitialParam(ProduceParams.FromIndex(p)), scenario.ParamCap);
            }

            for (var i = 0; i < supports.Length; i++)
            {
                var card = supports[i];
                var level = input.SupportCards[i].Level;
                for (var a = 0; a < card.Abilities.Count; a++)
                {
                    var ability = card.Abilities[a];
                    if (ability.Kind != SupportAbilityKind.InitialParam)
                    {
                        continue;
                    }

                    var value = ProduceFormulas.AbilityValue(ability.ValueAtLv1, ability.ValueAtMax, level, card.MaxLevelAtFullLimitBreak);
                    var index = ProduceParams.ToIndex(ability.Param);
                    parameters[index] = ProduceFormulas.AddParam(parameters[index], value, scenario.ParamCap);
                }
            }

            var cardIds = new List<int>();
            var common = new List<int>(catalog.CommonDeckCardIds);
            common.Sort();
            cardIds.AddRange(common);
            cardIds.Add(idol.UniqueCardId);
            var recommend = new List<int>(idol.RecommendCardIds);
            recommend.Sort();
            cardIds.AddRange(recommend);

            var deck = new List<CardInstance>();
            for (var i = 0; i < cardIds.Count; i++)
            {
                deck.Add(new CardInstance(i + 1, cardIds[i], false));
            }

            var state = new ProduceState(input, maxHp, parameters, bonus, deck, scenario.RetryCount);
            BeginWeek(state, streams);
            return state;
        }

        /// <summary>1 回の操作を受け、次の入力待ちまで進める。Accepted 以外のときは状態も記録も変えない。</summary>
        public ProduceApplyResult Apply(ProduceState state, ProduceCommand command, IRandomStreamSource streams)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (streams == null)
            {
                throw new ArgumentNullException(nameof(streams));
            }

            // 1. 種類の判定。MVP で受けない種類と、定義のない数値は InvalidArgument
            switch (command.Kind)
            {
                case OperationKind.ChooseWeekAction:
                    if (state.Phase != ProducePhase.WeekSelect)
                    {
                        return ProduceApplyResult.UnexpectedPhase;
                    }

                    break;
                case OperationKind.OfferPick:
                case OperationKind.OfferReroll:
                case OperationKind.OfferRemove:
                case OperationKind.OfferDecline:
                    if (state.Phase != ProducePhase.CardOffer)
                    {
                        return ProduceApplyResult.UnexpectedPhase;
                    }

                    break;
                case OperationKind.ExamRetry:
                case OperationKind.ExamNoRetry:
                    if (state.Phase != ProducePhase.ExamResult)
                    {
                        return ProduceApplyResult.UnexpectedPhase;
                    }

                    break;
                case OperationKind.UseCard:
                case OperationKind.UseDrink:
                case OperationKind.Skip:
                    if (state.Phase != ProducePhase.Lesson && state.Phase != ProducePhase.Exam)
                    {
                        return ProduceApplyResult.UnexpectedPhase;
                    }

                    break;
                default:
                    return ProduceApplyResult.InvalidArgument;
            }

            // 2. 受け付ける前の値を控える
            var weekBefore = state.Week;
            var attemptBefore = state.Attempt;
            var turnBefore = state.Lesson == null ? 0 : state.Lesson.Turn;

            // 3. 局面ごとの処理。拒否するときは状態を変える前に返る。
            // 処理の途中で例外が出たときの state は不定（呼び出し側は保存済みの状態から取り直す）
            ProduceApplyResult result;
            switch (state.Phase)
            {
                case ProducePhase.WeekSelect:
                    result = ApplyWeekSelect(state, command, streams);
                    break;
                case ProducePhase.CardOffer:
                    result = ApplyCardOffer(state, command, streams);
                    break;
                case ProducePhase.ExamResult:
                    result = ApplyExamResult(state, command, streams);
                    break;
                default:
                    result = ApplyLesson(state, command, streams);
                    break;
            }

            if (result != ProduceApplyResult.Accepted)
            {
                return result;
            }

            state.AddOperation(new OperationRecord(state.NextOperationNumber, command, weekBefore, attemptBefore, turnBefore));
            state.NextOperationNumber++;
            return ProduceApplyResult.Accepted;
        }

        /// <summary>週の選択の枠（クライアントの表示にも使う）。Phase が WeekSelect でなければ InvalidOperationException。</summary>
        public IReadOnlyList<WeekChoice> GetWeekChoices(ProduceState state)
        {
            RequireWeekSelect(state);
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            var choices = new List<WeekChoice>();
            if (week.Kind == WeekKind.Finishing)
            {
                choices.Add(new WeekChoice(0, WeekChoiceKind.Lesson, true));
                return choices;
            }

            for (var i = 0; i < week.Actions.Count; i++)
            {
                WeekChoiceKind kind;
                switch (week.Actions[i])
                {
                    case ActionKind.Lesson:
                        kind = WeekChoiceKind.Lesson;
                        break;
                    case ActionKind.Class:
                        kind = WeekChoiceKind.Class;
                        break;
                    case ActionKind.Supply:
                        kind = WeekChoiceKind.Supply;
                        break;
                    default:
                        throw new InvalidOperationException("MVP で扱わない行動の枠です: " + week.Actions[i]);
                }

                choices.Add(new WeekChoice(i, kind, true));
            }

            choices.Add(new WeekChoice(week.Actions.Count, WeekChoiceKind.Rest, state.Hp < state.MaxHp));
            return choices;
        }

        /// <summary>授業の選択肢。Phase が WeekSelect でなければ InvalidOperationException。授業の枠が無い週は空。</summary>
        public IReadOnlyList<ClassOptionView> GetClassOptions(ProduceState state)
        {
            RequireWeekSelect(state);
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            var views = new List<ClassOptionView>();
            for (var i = 0; i < week.ClassOptions.Count; i++)
            {
                var option = week.ClassOptions[i];
                var selectable = option.HpCost == 0 || state.Hp > option.HpCost;
                views.Add(new ClassOptionView(i, option.HpCost, (ParamKind)state.ClassOptionParams[i], option.ParamGain, option.GivesCard, selectable));
            }

            return views;
        }

        /// <summary>レッスン・試験中のカードの使用可否。Lesson が null なら NotPlayerPhase。</summary>
        public UseCheckResult CanUse(ProduceState state, int instanceId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Lesson == null)
            {
                return UseCheckResult.NotPlayerPhase;
            }

            return lessonEngine.CanUse(state.Lesson, instanceId);
        }

        private static void RequireWeekSelect(ProduceState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Phase != ProducePhase.WeekSelect)
            {
                throw new InvalidOperationException("週の選択の局面ではありません。");
            }
        }

        /// <summary>週の始まり。state.Week の週。</summary>
        private void BeginWeek(ProduceState state, IRandomStreamSource streams)
        {
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            switch (week.Kind)
            {
                case WeekKind.Action:
                    if (week.ClassOptions.Count > 0)
                    {
                        var random = streams.Get(new RandomStreamKey(RandomStreamKind.Week, state.Week, 0));
                        var picked = new int[week.ClassOptions.Count];
                        for (var i = 0; i < picked.Length; i++)
                        {
                            picked[i] = (int)ProduceParams.FromIndex(RandomOps.NextInt(random, ProduceRules.ParamCount));
                        }

                        state.SetClassOptionParams(picked);
                    }
                    else
                    {
                        state.ClearClassOptionParams();
                    }

                    state.Phase = ProducePhase.WeekSelect;
                    break;
                case WeekKind.Finishing:
                    state.ClearClassOptionParams();
                    state.Phase = ProducePhase.WeekSelect;
                    break;
                default:
                    // MidExam・FinalExam
                    state.Attempt = 0;
                    state.ClearClassOptionParams();
                    state.ExamBackup = new ExamBackup(state.CopyParams(), state.Hp, state.Deck, state.NextInstanceId);
                    StartExam(state, streams);
                    break;
            }
        }

        private ProduceApplyResult ApplyWeekSelect(ProduceState state, ProduceCommand command, IRandomStreamSource streams)
        {
            var choices = GetWeekChoices(state);
            if (command.Arg1 < 0 || command.Arg1 >= choices.Count || !choices[command.Arg1].Selectable)
            {
                return ProduceApplyResult.InvalidArgument;
            }

            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            switch (choices[command.Arg1].Kind)
            {
                case WeekChoiceKind.Lesson:
                {
                    var param = (ParamKind)command.Arg3;
                    if (!ProduceParams.IsValid(param))
                    {
                        return ProduceApplyResult.InvalidArgument;
                    }

                    var kind = week.Kind == WeekKind.Finishing ? LessonKind.Finishing : LessonKind.Normal;
                    StartLesson(state, streams, kind, param);
                    return ProduceApplyResult.Accepted;
                }

                case WeekChoiceKind.Class:
                {
                    if (command.Arg2 < 0 || command.Arg2 >= week.ClassOptions.Count)
                    {
                        return ProduceApplyResult.InvalidArgument;
                    }

                    var options = GetClassOptions(state);
                    if (!options[command.Arg2].Selectable)
                    {
                        return ProduceApplyResult.InvalidArgument;
                    }

                    var option = week.ClassOptions[command.Arg2];
                    var param = (ParamKind)state.ClassOptionParams[command.Arg2];
                    state.Hp -= option.HpCost;

                    // 授業ではパラメータボーナスを掛けない（produce.md 3.4）
                    state.SetParam(param, ProduceFormulas.AddParam(state.GetParam(param), option.ParamGain, scenario.ParamCap));
                    state.ClearClassOptionParams();
                    if (option.GivesCard)
                    {
                        DrawOffer(state, streams);
                    }
                    else
                    {
                        EndWeek(state, streams);
                    }

                    return ProduceApplyResult.Accepted;
                }

                case WeekChoiceKind.Supply:
                    state.ClearClassOptionParams();
                    DrawOffer(state, streams);
                    return ProduceApplyResult.Accepted;
                case WeekChoiceKind.Rest:
                    state.Hp = ProduceFormulas.Heal(state.Hp, ProduceFormulas.RestAmount(state.MaxHp), state.MaxHp);
                    state.ClearClassOptionParams();
                    EndWeek(state, streams);
                    return ProduceApplyResult.Accepted;
                default:
                    return ProduceApplyResult.InvalidArgument;
            }
        }

        private void StartLesson(ProduceState state, IRandomStreamSource streams, LessonKind kind, ParamKind param)
        {
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            var idol = catalog.GetIdol(state.Input.IdolId);
            state.ClearClassOptionParams();

            var setup = new LessonSetup(kind, param, week.GetTurns(idol.Plan), week.ClearTarget, week.PerfectTarget);
            var lesson = new LessonState(setup, state.Deck, state.Hp, state.MaxHp);
            var random = streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, state.Week, 0));
            lessonEngine.Start(lesson, random);
            state.Lesson = lesson;
            state.Phase = ProducePhase.Lesson;
            if (lesson.IsFinished)
            {
                FinishLesson(state, streams);
            }
        }

        private ProduceApplyResult ApplyLesson(ProduceState state, ProduceCommand command, IRandomStreamSource streams)
        {
            LessonCommand lessonCommand;
            switch (command.Kind)
            {
                case OperationKind.UseCard:
                    lessonCommand = LessonCommand.UseCard(command.Arg1);
                    break;
                case OperationKind.UseDrink:
                    lessonCommand = LessonCommand.UseDrink(command.Arg1);
                    break;
                default:
                    lessonCommand = LessonCommand.Skip();
                    break;
            }

            var random = streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, state.Week, state.Phase == ProducePhase.Exam ? state.Attempt : 0));
            var applied = lessonEngine.Apply(state.Lesson, lessonCommand, random);
            if (applied != LessonApplyResult.Accepted)
            {
                return ProduceApplyResult.InvalidArgument;
            }

            if (state.Lesson.IsFinished)
            {
                if (state.Phase == ProducePhase.Lesson)
                {
                    FinishLesson(state, streams);
                }
                else
                {
                    FinishExam(state, streams);
                }
            }

            return ProduceApplyResult.Accepted;
        }

        private void FinishLesson(ProduceState state, IRandomStreamSource streams)
        {
            var lesson = state.Lesson;
            var result = LessonEngine.GetResult(lesson);
            var setup = lesson.Setup;
            var idol = catalog.GetIdol(state.Input.IdolId);
            var scenario = catalog.GetScenario(state.Input.ScenarioId);

            var alloc = ProduceFormulas.DistributeGain(
                setup.Kind == LessonKind.Finishing, setup.TargetParam, result.Gained, setup.ClearTarget, idol.FirstParam);
            for (var i = 0; i < alloc.Length; i++)
            {
                var param = ProduceParams.FromIndex(i);
                var add = alloc[i] + ProduceFormulas.BonusAmount(alloc[i], state.GetBonusPerThousand(param));
                state.SetParam(param, ProduceFormulas.AddParam(state.GetParam(param), add, scenario.ParamCap));
            }

            var hp = result.Hp;
            if (result.Reason == LessonEndReason.Perfect)
            {
                hp = ProduceFormulas.Heal(hp, ProduceFormulas.PerfectRecover(setup.MaxTurns, result.LastTurn), state.MaxHp);
            }

            if (setup.Kind == LessonKind.Finishing)
            {
                hp = ProduceFormulas.Heal(hp, ProduceFormulas.RestAmount(state.MaxHp), state.MaxHp);
            }

            state.Hp = hp;
            state.Lesson = null;
            if (result.Gained >= setup.ClearTarget)
            {
                DrawOffer(state, streams);
            }
            else
            {
                EndWeek(state, streams);
            }
        }

        private void DrawOffer(ProduceState state, IRandomStreamSource streams)
        {
            var idol = catalog.GetIdol(state.Input.IdolId);
            var deckCardIds = new List<int>();
            for (var i = 0; i < state.Deck.Count; i++)
            {
                deckCardIds.Add(state.Deck[i].CardId);
            }

            var candidates = OfferPool.GetCandidates(catalog, idol, state.Input.PlayerLevel, deckCardIds);
            var random = streams.Get(new RandomStreamKey(RandomStreamKind.Offer, state.Week, 0));
            var cards = OfferPool.Draw(catalog, candidates, random);
            if (cards.Count == 0)
            {
                EndWeek(state, streams);
                return;
            }

            state.SetOfferCards(cards);
            state.Phase = ProducePhase.CardOffer;
        }

        private ProduceApplyResult ApplyCardOffer(ProduceState state, ProduceCommand command, IRandomStreamSource streams)
        {
            switch (command.Kind)
            {
                case OperationKind.OfferPick:
                    if (command.Arg1 < 0 || command.Arg1 >= state.OfferCards.Count)
                    {
                        return ProduceApplyResult.InvalidArgument;
                    }

                    state.AddCard(new CardInstance(state.NextInstanceId, state.OfferCards[command.Arg1], false));
                    state.NextInstanceId++;
                    state.ClearOfferCards();
                    EndWeek(state, streams);
                    return ProduceApplyResult.Accepted;
                case OperationKind.OfferDecline:
                    state.ClearOfferCards();
                    EndWeek(state, streams);
                    return ProduceApplyResult.Accepted;
                default:
                    // 再抽選・除去の残り回数は MVP では 0（produce.md 3.5）
                    return ProduceApplyResult.InvalidArgument;
            }
        }

        /// <summary>入力待ちではない。次の週の始まりまで進める。</summary>
        private void EndWeek(ProduceState state, IRandomStreamSource streams)
        {
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            if (state.Week >= scenario.WeekCount)
            {
                throw new InvalidOperationException("最終週は試験で終わります。");
            }

            state.ClearOfferCards();
            state.ClearClassOptionParams();
            state.Week++;
            BeginWeek(state, streams);
        }

        private ProduceApplyResult ApplyExamResult(ProduceState state, ProduceCommand command, IRandomStreamSource streams)
        {
            if (command.Kind == OperationKind.ExamNoRetry)
            {
                ConcludeExam(state, streams);
                return ProduceApplyResult.Accepted;
            }

            // ExamRetry: 試験の開始直前の育成の区分に戻す
            var backup = state.ExamBackup;
            for (var i = 0; i < backup.Parameters.Count; i++)
            {
                state.SetParam(ProduceParams.FromIndex(i), backup.Parameters[i]);
            }

            state.Hp = backup.Hp;
            state.ReplaceDeck(backup.Deck);
            state.NextInstanceId = backup.NextInstanceId;
            state.ExamRetryLeft--;
            state.Attempt++;
            StartExam(state, streams);
            return ProduceApplyResult.Accepted;
        }

        private static ExamKind GetExamKind(WeekDefinition week)
        {
            return week.Kind == WeekKind.MidExam ? ExamKind.Mid : ExamKind.Final;
        }

        private void StartExam(ProduceState state, IRandomStreamSource streams)
        {
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            var idol = catalog.GetIdol(state.Input.IdolId);
            var random = streams.Get(new RandomStreamKey(RandomStreamKind.Lesson, state.Week, state.Attempt));
            var turns = ExamTurnBuilder.Build(catalog, idol, GetExamKind(week), state.CopyParams(), random);
            var setup = new LessonSetup(
                week.Kind == WeekKind.MidExam ? LessonKind.MidExam : LessonKind.FinalExam,
                ParamKind.Nothing,
                turns.Count,
                0,
                0,
                new ExamSetup(turns, week.ExamEarlyEndScore));
            var lesson = new LessonState(setup, state.Deck, state.Hp, state.MaxHp);
            lessonEngine.Start(lesson, random);
            state.Lesson = lesson;
            state.Phase = ProducePhase.Exam;
            if (lesson.IsFinished)
            {
                FinishExam(state, streams);
            }
        }

        private void FinishExam(ProduceState state, IRandomStreamSource streams)
        {
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var week = scenario.GetWeek(state.Week);
            var examKind = GetExamKind(week);
            var result = LessonEngine.GetResult(state.Lesson);
            var npc = ProduceFormulas.NpcScores(week.RivalBaseScore);
            var rank = ProduceFormulas.ExamRank(result.Gained, npc);
            var passed = rank <= ProduceRules.PassRank;
            state.LastExam = new ExamReport(examKind, state.Attempt, result.Gained, rank, npc, passed);
            state.Hp = result.Hp;
            state.Lesson = null;
            if (state.ExamRetryLeft > 0)
            {
                // 合否にかかわらず再挑戦を選べる（produce.md 5.1.1）
                state.Phase = ProducePhase.ExamResult;
                return;
            }

            ConcludeExam(state, streams);
        }

        private void ConcludeExam(ProduceState state, IRandomStreamSource streams)
        {
            var scenario = catalog.GetScenario(state.Input.ScenarioId);
            var report = state.LastExam;
            state.Attempt = 0;
            if (report.Kind == ExamKind.Mid)
            {
                state.MidExam = report;
                if (!report.Passed)
                {
                    FinishProduce(state, ExamOutcome.MidFailed);
                    return;
                }

                AddRankReward(state, scenario, report);
                state.ExamBackup = null;
                EndWeek(state, streams);
                return;
            }

            if (report.Passed)
            {
                AddRankReward(state, scenario, report);
                FinishProduce(state, ExamOutcome.FinalPassed);
                return;
            }

            FinishProduce(state, ExamOutcome.FinalFailed);
        }

        private void AddRankReward(ProduceState state, ScenarioDefinition scenario, ExamReport report)
        {
            var reward = catalog.GetExamRankParamAll(report.Kind, report.Rank);
            for (var i = 0; i < ProduceRules.ParamCount; i++)
            {
                var param = ProduceParams.FromIndex(i);
                state.SetParam(param, ProduceFormulas.AddParam(state.GetParam(param), reward, scenario.ParamCap));
            }
        }

        private void FinishProduce(ProduceState state, ExamOutcome outcome)
        {
            var finalTaken = outcome != ExamOutcome.MidFailed;
            var finalRank = finalTaken ? state.LastExam.Rank : 0;
            var finalScore = finalTaken ? state.LastExam.Score : 0;
            var paramSum = state.GetParam(ParamKind.Singing) + state.GetParam(ParamKind.Rhythm) + state.GetParam(ParamKind.Expression);
            var eval = ProduceFormulas.Evaluate(catalog, paramSum, finalRank, finalScore);
            state.Result = new ProduceResult(outcome, eval.Value, eval.Rank, finalScore, finalRank);
            state.ExamBackup = null;
            state.Phase = ProducePhase.Finished;
        }
    }
}

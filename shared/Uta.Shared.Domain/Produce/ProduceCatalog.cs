using System;
using System.Collections.Generic;
using System.Linq;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>
    /// マスタの行から組み立てた、育成の定義。構造の検査はここで行い、クライアントとサーバーで食い違わないようにする
    /// （値の大小の妥当性はマスタの IValidatable の担当）。
    /// </summary>
    public sealed class ProduceCatalog
    {
        private const int RankCount = 13;

        private readonly Dictionary<int, ScenarioDefinition> scenarios;
        private readonly Dictionary<int, IdolDefinition> idols;
        private readonly Dictionary<int, SupportCardDefinition> supportCards;
        private readonly Dictionary<int, ExamPatternDefinition> examPatterns;
        private readonly Dictionary<Rarity, int> offerWeights;
        private readonly Dictionary<long, int> maxLevels;
        private readonly Dictionary<long, int> examRankParamAll;
        private readonly int scoreBonusA;
        private readonly int[] scoreBonusB;

        private ProduceCatalog(
            ICardCatalog cards,
            Dictionary<int, ScenarioDefinition> scenarios,
            Dictionary<int, IdolDefinition> idols,
            Dictionary<int, SupportCardDefinition> supportCards,
            Dictionary<int, ExamPatternDefinition> examPatterns,
            IReadOnlyList<int> commonDeckCardIds,
            Dictionary<Rarity, int> offerWeights,
            Dictionary<long, int> maxLevels,
            Dictionary<long, int> examRankParamAll,
            int scoreBonusA,
            int[] scoreBonusB,
            IReadOnlyList<EvaluationScoreBandRow> scoreBands,
            IReadOnlyList<EvaluationRankRow> evaluationRanks)
        {
            Cards = cards;
            this.scenarios = scenarios;
            this.idols = idols;
            this.supportCards = supportCards;
            this.examPatterns = examPatterns;
            CommonDeckCardIds = commonDeckCardIds;
            this.offerWeights = offerWeights;
            this.maxLevels = maxLevels;
            this.examRankParamAll = examRankParamAll;
            this.scoreBonusA = scoreBonusA;
            this.scoreBonusB = scoreBonusB;
            EvaluationScoreBands = scoreBands;
            EvaluationRanks = evaluationRanks;
        }

        public ICardCatalog Cards { get; }

        /// <summary>CommonDeckCard の Order の昇順</summary>
        public IReadOnlyList<int> CommonDeckCardIds { get; }

        /// <summary>ScoreBonusSetting の APerThousand</summary>
        public int ScoreBonusA => scoreBonusA;

        /// <summary>Order の昇順</summary>
        public IReadOnlyList<EvaluationScoreBandRow> EvaluationScoreBands { get; }

        /// <summary>MinValue の昇順（F から SSPlus）</summary>
        public IReadOnlyList<EvaluationRankRow> EvaluationRanks { get; }

        /// <summary>行から組み立てる。問題があれば InvalidOperationException（メッセージに問題を全部、改行区切りで入れる）。</summary>
        public static ProduceCatalog Create(ProduceMasterRows rows, ICardCatalog cards)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            return new Builder(rows, cards).Build();
        }

        public ScenarioDefinition GetScenario(int scenarioId)
        {
            return scenarios[scenarioId];
        }

        public IdolDefinition GetIdol(int idolId)
        {
            return idols[idolId];
        }

        public SupportCardDefinition GetSupportCard(int supportCardId)
        {
            return supportCards[supportCardId];
        }

        public ExamPatternDefinition GetExamPattern(int examPatternId)
        {
            return examPatterns[examPatternId];
        }

        /// <summary>OfferRarityWeight の Weight</summary>
        public int GetOfferWeight(Rarity rarity)
        {
            return offerWeights[rarity];
        }

        /// <summary>SupportLevelCap の MaxLevel。無ければ ArgumentOutOfRangeException</summary>
        public int GetMaxLevel(Rarity rarity, int limitBreak)
        {
            if (!maxLevels.TryGetValue(Key((int)rarity, limitBreak), out var value))
            {
                throw new ArgumentOutOfRangeException(nameof(limitBreak));
            }

            return value;
        }

        /// <summary>ExamRankReward の ParamAll。rank が 1〜3 でない、または行が無ければ 0</summary>
        public int GetExamRankParamAll(ExamKind kind, int rank)
        {
            if (rank < 1 || rank > 3)
            {
                return 0;
            }

            return examRankParamAll.TryGetValue(Key((int)kind, rank), out var value) ? value : 0;
        }

        /// <summary>1 → BFirst、2 → BSecond、3 → BThird。それ以外は ArgumentOutOfRangeException</summary>
        public int GetScoreBonusB(int rank)
        {
            if (rank < 1 || rank > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            return scoreBonusB[rank - 1];
        }

        // 複合キーを long に詰める（各要素は 0 以上 2^20 未満。AOT で ValueTuple の比較子が作られないようにする）
        private static long Key(int a, int b)
        {
            return ((long)a << 20) | (long)b;
        }

        private static long Key(int a, int b, int c)
        {
            return ((long)a << 40) | ((long)b << 20) | (long)c;
        }

        private sealed class Builder
        {
            private readonly ProduceMasterRows rows;
            private readonly ICardCatalog cards;
            private readonly HashSet<int> cardIds;
            private readonly List<string> problems = new List<string>();
            private readonly HashSet<string> seenProblems = new HashSet<string>();

            public Builder(ProduceMasterRows rows, ICardCatalog cards)
            {
                this.rows = rows;
                this.cards = cards;
                cardIds = new HashSet<int>(cards.CardIds);
            }

            public ProduceCatalog Build()
            {
                // V1: 主キーの重複
                var scenarioRows = Index(rows.Scenarios, r => r.ScenarioId, k => "Scenario " + k);
                var weekRows = IndexComposite(rows.ScenarioWeeks, r => Key(r.ScenarioId, r.Week), r => "ScenarioWeek " + r.ScenarioId + " week " + r.Week);
                var slotRows = IndexComposite(rows.WeekActionSlots, r => Key(r.ScenarioId, r.Week, r.Order), r => "WeekActionSlot " + r.ScenarioId + " week " + r.Week + " order " + r.Order);
                var optionRows = IndexComposite(rows.ClassOptions, r => Key(r.ScenarioId, r.Week, r.Order), r => "ClassOption " + r.ScenarioId + " week " + r.Week + " order " + r.Order);
                var patternRows = Index(rows.ExamPatterns, r => r.ExamPatternId, k => "ExamPattern " + k);
                var idolRows = Index(rows.Idols, r => r.IdolId, k => "Idol " + k);
                var supportRows = Index(rows.SupportCards, r => r.SupportCardId, k => "SupportCard " + k);
                var capRows = IndexComposite(rows.SupportLevelCaps, r => Key((int)r.Rarity, r.LimitBreak), r => "SupportLevelCap " + r.Rarity + "/" + r.LimitBreak);
                var commonRows = Index(rows.CommonDeckCards, r => r.Order, k => "CommonDeckCard " + k);
                var recommendRows = IndexComposite(rows.IdolRecommendDecks, r => Key((int)r.Recommend, r.Order), r => "IdolRecommendDeck " + r.Recommend + "/" + r.Order);
                var abilityRows = IndexComposite(rows.SupportAbilities, r => Key(r.SupportCardId, r.Order), r => "SupportAbility " + r.SupportCardId + "/" + r.Order);
                var rewardRows = IndexComposite(rows.ExamRankRewards, r => Key((int)r.ExamKind, r.Rank), r => "ExamRankReward " + r.ExamKind + "/" + r.Rank);
                var weightRows = Index(rows.OfferRarityWeights, r => r.Rarity, k => "OfferRarityWeight " + k);
                var bandRows = Index(rows.EvaluationScoreBands, r => r.Order, k => "EvaluationScoreBand " + k);
                var rankRows = Index(rows.EvaluationRanks, r => r.Rank, k => "EvaluationRank " + k);

                var patterns = BuildExamPatterns(patternRows);
                var scenarios = BuildScenarios(scenarioRows, weekRows, slotRows, optionRows);
                var idols = BuildIdols(idolRows, recommendRows, patternRows);
                var supports = BuildSupportCards(supportRows, abilityRows, capRows);
                var maxLevels = BuildMaxLevels(capRows);
                var commonDeck = BuildCommonDeck(commonRows);
                var weights = BuildOfferWeights(weightRows);
                var rewards = BuildRewards(rewardRows);
                var scoreBonus = CheckScoreBonus();
                var bands = BuildBands(bandRows);
                var ranks = BuildRanks(rankRows);

                if (problems.Count > 0)
                {
                    throw new InvalidOperationException(string.Join("\n", problems));
                }

                return new ProduceCatalog(
                    cards,
                    scenarios,
                    idols,
                    supports,
                    patterns,
                    commonDeck,
                    weights,
                    maxLevels,
                    rewards,
                    scoreBonus.APerThousand,
                    new[] { scoreBonus.BFirst, scoreBonus.BSecond, scoreBonus.BThird },
                    bands,
                    ranks);
            }

            private void Problem(string message)
            {
                if (seenProblems.Add(message))
                {
                    problems.Add(message);
                }
            }

            // 複合主キー版（キーは Key で詰めた long。ラベルは行から作る）。
            private Dictionary<long, TRow> IndexComposite<TRow>(IReadOnlyList<TRow> source, Func<TRow, long> key, Func<TRow, string> label)
            {
                var result = new Dictionary<long, TRow>();
                foreach (var row in source)
                {
                    var k = key(row);
                    if (result.ContainsKey(k))
                    {
                        Problem(label(row) + ": 重複");
                        continue;
                    }

                    result.Add(k, row);
                }

                return result;
            }

            // 主キーごとに引ける辞書にする。重複は問題にして、最初の行を残す。
            private Dictionary<TKey, TRow> Index<TRow, TKey>(IReadOnlyList<TRow> source, Func<TRow, TKey> key, Func<TKey, string> label)
            {
                var result = new Dictionary<TKey, TRow>();
                foreach (var row in source)
                {
                    var k = key(row);
                    if (result.ContainsKey(k))
                    {
                        Problem(label(k) + ": 重複");
                        continue;
                    }

                    result.Add(k, row);
                }

                return result;
            }

            // V4
            private Dictionary<int, ExamPatternDefinition> BuildExamPatterns(Dictionary<int, ExamPatternRow> patternRows)
            {
                var result = new Dictionary<int, ExamPatternDefinition>();
                foreach (var row in patternRows.Values)
                {
                    var name = "ExamPattern " + row.ExamPatternId;
                    var mid = new[] { row.MidFirstTurns, row.MidSecondTurns, row.MidThirdTurns };
                    var finalSense = new[] { row.FinalSenseFirstTurns, row.FinalSenseSecondTurns, row.FinalSenseThirdTurns };
                    var finalLogic = new[] { row.FinalLogicFirstTurns, row.FinalLogicSecondTurns, row.FinalLogicThirdTurns };
                    CheckTurnCounts(name + " Mid", mid, row.FirstTurnSecondRatePerTenThousand);
                    CheckTurnCounts(name + " FinalSense", finalSense, row.FirstTurnSecondRatePerTenThousand);
                    CheckTurnCounts(name + " FinalLogic", finalLogic, row.FirstTurnSecondRatePerTenThousand);
                    if (row.FirstTurnSecondRatePerTenThousand < 0 || row.FirstTurnSecondRatePerTenThousand > 10000)
                    {
                        Problem(name + ": FirstTurnSecondRatePerTenThousand が 0〜10000 の外");
                    }

                    result.Add(row.ExamPatternId, new ExamPatternDefinition(row.ExamPatternId, row.FirstTurnSecondRatePerTenThousand, mid, finalSense, finalLogic));
                }

                return result;
            }

            private void CheckTurnCounts(string name, int[] counts, int rate)
            {
                if (counts[0] < 2)
                {
                    Problem(name + ": 1 番のターン数が 2 未満");
                }

                if (counts[1] < 1)
                {
                    Problem(name + ": 2 番のターン数が 1 未満");
                }

                if (counts[2] < 1)
                {
                    Problem(name + ": 3 番のターン数が 1 未満");
                }

                if (rate >= 1 && counts[1] < 2)
                {
                    Problem(name + ": 1 ターン目が 2 番になる率が 1 以上なのに 2 番のターン数が 2 未満");
                }
            }

            // V2・V3
            private Dictionary<int, ScenarioDefinition> BuildScenarios(
                Dictionary<int, ScenarioRow> scenarioRows,
                Dictionary<long, ScenarioWeekRow> weekRows,
                Dictionary<long, WeekActionSlotRow> slotRows,
                Dictionary<long, ClassOptionRow> optionRows)
            {
                foreach (var week in weekRows.Values)
                {
                    if (!scenarioRows.TryGetValue(week.ScenarioId, out var owner))
                    {
                        Problem("ScenarioWeek " + week.ScenarioId + " week " + week.Week + ": Scenario がない");
                    }
                    else if (week.Week < 1 || week.Week > owner.WeekCount)
                    {
                        Problem("Scenario " + week.ScenarioId + " week " + week.Week + ": WeekCount を超える週");
                    }
                }

                foreach (var slot in slotRows.Values)
                {
                    if (!weekRows.ContainsKey(Key(slot.ScenarioId, slot.Week)))
                    {
                        Problem("WeekActionSlot " + slot.ScenarioId + " week " + slot.Week + ": ScenarioWeek がない");
                    }
                }

                foreach (var option in optionRows.Values)
                {
                    if (!weekRows.ContainsKey(Key(option.ScenarioId, option.Week)))
                    {
                        Problem("ClassOption " + option.ScenarioId + " week " + option.Week + ": ScenarioWeek がない");
                    }
                }

                var slotsByWeek = slotRows.Values
                    .GroupBy(x => Key(x.ScenarioId, x.Week))
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToArray());
                var optionsByWeek = optionRows.Values
                    .GroupBy(x => Key(x.ScenarioId, x.Week))
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToArray());

                var result = new Dictionary<int, ScenarioDefinition>();
                foreach (var scenario in scenarioRows.Values)
                {
                    var name = "Scenario " + scenario.ScenarioId;
                    if (scenario.WeekCount < 1)
                    {
                        Problem(name + ": WeekCount が 1 未満");
                    }

                    if (scenario.ParamCap < 1)
                    {
                        Problem(name + ": ParamCap が 1 未満");
                    }

                    if (scenario.RetryCount < 0)
                    {
                        Problem(name + ": RetryCount が負");
                    }

                    var weeks = new List<WeekDefinition>();
                    for (var w = 1; w <= scenario.WeekCount; w++)
                    {
                        if (!weekRows.TryGetValue(Key(scenario.ScenarioId, w), out var weekRow))
                        {
                            Problem(name + " week " + w + ": 週がない");
                            continue;
                        }

                        slotsByWeek.TryGetValue(Key(scenario.ScenarioId, w), out var slots);
                        optionsByWeek.TryGetValue(Key(scenario.ScenarioId, w), out var options);
                        weeks.Add(BuildWeek(name + " week " + w, weekRow, slots ?? new WeekActionSlotRow[0], options ?? new ClassOptionRow[0]));
                    }

                    if (weekRows.TryGetValue(Key(scenario.ScenarioId, scenario.WeekCount), out var last) && last.Kind != WeekKind.FinalExam)
                    {
                        Problem(name + ": 最後の週: FinalExam でなければならない");
                    }

                    result.Add(
                        scenario.ScenarioId,
                        new ScenarioDefinition(scenario.ScenarioId, scenario.Name, scenario.Difficulty, scenario.WeekCount, scenario.ParamCap, scenario.RetryCount, weeks));
                }

                return result;
            }

            private WeekDefinition BuildWeek(string name, ScenarioWeekRow week, WeekActionSlotRow[] slots, ClassOptionRow[] options)
            {
                var actions = slots.Select(x => x.Kind).ToArray();
                var classOptions = options.Select(x => new ClassOptionDefinition(x.HpCost, x.ParamGain, x.GivesCard)).ToArray();

                if (week.Kind == WeekKind.Nothing)
                {
                    Problem(name + ": Kind が Nothing");
                }

                foreach (var kind in actions)
                {
                    if (kind != ActionKind.Lesson && kind != ActionKind.Class && kind != ActionKind.Supply)
                    {
                        Problem(name + ": 未対応の行動 " + kind);
                    }
                }

                if (actions.Length != actions.Distinct().Count())
                {
                    Problem(name + ": 同じ種類の行動の枠が 2 つ以上ある");
                }

                var isExam = week.Kind == WeekKind.MidExam || week.Kind == WeekKind.FinalExam;
                if (week.Kind == WeekKind.Action && actions.Length < 1)
                {
                    Problem(name + ": Action の週なのに行動の枠がない");
                }

                if (week.Kind == WeekKind.Finishing && (actions.Length != 1 || actions[0] != ActionKind.Lesson))
                {
                    Problem(name + ": Finishing の週は Lesson の枠がちょうど 1 つ必要");
                }

                if (isExam && actions.Length > 0)
                {
                    Problem(name + ": 試験の週に行動の枠がある");
                }

                if (actions.Contains(ActionKind.Lesson) &&
                    (week.TurnsSense < 1 || week.TurnsLogic < 1 || week.ClearTarget < 1 || week.PerfectTarget < 1))
                {
                    Problem(name + ": Lesson の週は TurnsSense・TurnsLogic・ClearTarget・PerfectTarget が 1 以上必要");
                }

                if (actions.Contains(ActionKind.Class))
                {
                    if (classOptions.Length < 1)
                    {
                        Problem(name + ": Class の枠があるのに ClassOption がない");
                    }
                }
                else if (classOptions.Length > 0)
                {
                    Problem(name + ": Class の枠がないのに ClassOption がある");
                }

                if (isExam && week.RivalBaseScore < 1)
                {
                    Problem(name + ": 試験の週は RivalBaseScore が 1 以上必要");
                }

                return new WeekDefinition(
                    week.Week,
                    week.Kind,
                    week.TurnsSense,
                    week.TurnsLogic,
                    week.ClearTarget,
                    week.PerfectTarget,
                    week.ExamEarlyEndScore,
                    week.RivalBaseScore,
                    actions,
                    classOptions);
            }

            // V5
            private Dictionary<int, IdolDefinition> BuildIdols(
                Dictionary<int, IdolRow> idolRows,
                Dictionary<long, IdolRecommendDeckRow> recommendRows,
                Dictionary<int, ExamPatternRow> patternRows)
            {
                var result = new Dictionary<int, IdolDefinition>();
                foreach (var idol in idolRows.Values)
                {
                    var name = "Idol " + idol.IdolId;
                    if (idol.Plan != Plan.Sense && idol.Plan != Plan.Logic)
                    {
                        Problem(name + ": Plan が Sense・Logic のどちらでもない");
                    }

                    if (idol.Rarity == Rarity.Nothing)
                    {
                        Problem(name + ": Rarity が Nothing");
                    }

                    if (idol.MaxHp < 1)
                    {
                        Problem(name + ": MaxHp が 1 未満");
                    }

                    if (idol.InitialSinging < 0 || idol.InitialRhythm < 0 || idol.InitialExpression < 0)
                    {
                        Problem(name + ": 初期パラメータが負");
                    }

                    if (idol.BonusSingingPerThousand < 0 || idol.BonusRhythmPerThousand < 0 || idol.BonusExpressionPerThousand < 0)
                    {
                        Problem(name + ": パラメータのボーナスが負");
                    }

                    if (!patternRows.ContainsKey(idol.ExamPatternId))
                    {
                        Problem(name + ": ExamPatternId " + idol.ExamPatternId + " がない");
                    }

                    var order = new[] { idol.FirstParam, idol.SecondParam, idol.ThirdParam };
                    if (!order.All(ProduceParams.IsValid) || order.Distinct().Count() != order.Length)
                    {
                        Problem(name + ": 審査の順が 3 つのパラメータの並べ替えでない");
                    }

                    if (!cardIds.Contains(idol.UniqueCardId))
                    {
                        Problem(name + ": UniqueCardId " + idol.UniqueCardId + " がカードにない");
                    }

                    var recommend = new List<int>();
                    for (var o = 1; o <= 3; o++)
                    {
                        if (!recommendRows.TryGetValue(Key((int)idol.Recommend, o), out var deckRow))
                        {
                            Problem(name + ": おすすめデッキ " + idol.Recommend + " の Order " + o + " の行がない");
                            continue;
                        }

                        if (!cardIds.Contains(deckRow.CardId))
                        {
                            Problem(name + ": おすすめデッキの CardId " + deckRow.CardId + " がカードにない");
                        }

                        recommend.Add(deckRow.CardId);
                    }

                    result.Add(
                        idol.IdolId,
                        new IdolDefinition(
                            idol.IdolId,
                            idol.Name,
                            idol.Rarity,
                            idol.Plan,
                            idol.Recommend,
                            idol.MaxHp,
                            idol.ExamPatternId,
                            idol.FirstParam,
                            idol.SecondParam,
                            idol.ThirdParam,
                            idol.UniqueCardId,
                            recommend,
                            new[] { idol.InitialSinging, idol.InitialRhythm, idol.InitialExpression },
                            new[] { idol.BonusSingingPerThousand, idol.BonusRhythmPerThousand, idol.BonusExpressionPerThousand }));
                }

                return result;
            }

            // V6
            private Dictionary<int, SupportCardDefinition> BuildSupportCards(
                Dictionary<int, SupportCardRow> supportRows,
                Dictionary<long, SupportAbilityRow> abilityRows,
                Dictionary<long, SupportLevelCapRow> capRows)
            {
                foreach (var cap in capRows.Values)
                {
                    if (cap.MaxLevel < 2)
                    {
                        Problem("SupportLevelCap " + cap.Rarity + "/" + cap.LimitBreak + ": MaxLevel が 2 未満");
                    }
                }

                foreach (var ability in abilityRows.Values)
                {
                    var name = "SupportAbility " + ability.SupportCardId + "/" + ability.Order;
                    if (!supportRows.ContainsKey(ability.SupportCardId))
                    {
                        Problem(name + ": SupportCard " + ability.SupportCardId + " がない");
                    }

                    if (ability.Kind == SupportAbilityKind.Nothing)
                    {
                        Problem(name + ": Kind が Nothing");
                    }
                    else if (ability.Kind == SupportAbilityKind.MaxHp)
                    {
                        if (ability.Param != ParamKind.Nothing)
                        {
                            Problem(name + ": MaxHp の行は Param が Nothing でなければならない");
                        }
                    }
                    else if (!ProduceParams.IsValid(ability.Param))
                    {
                        Problem(name + ": Param が歌唱・律動・表現のどれでもない");
                    }

                    if (ability.ValueAtLv1 < 0)
                    {
                        Problem(name + ": ValueAtLv1 が負");
                    }

                    if (ability.ValueAtMax < ability.ValueAtLv1)
                    {
                        Problem(name + ": ValueAtMax が ValueAtLv1 未満");
                    }
                }

                var abilitiesByCard = abilityRows.Values
                    .GroupBy(x => x.SupportCardId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToArray());

                var result = new Dictionary<int, SupportCardDefinition>();
                foreach (var card in supportRows.Values)
                {
                    var name = "SupportCard " + card.SupportCardId;
                    if (card.Rarity == Rarity.Nothing)
                    {
                        Problem(name + ": Rarity が Nothing");
                    }
                    else
                    {
                        for (var lb = 0; lb <= ProduceRules.MaxLimitBreak; lb++)
                        {
                            if (!capRows.ContainsKey(Key((int)card.Rarity, lb)))
                            {
                                Problem(name + ": SupportLevelCap " + card.Rarity + "/" + lb + " がない");
                            }
                        }
                    }

                    if (card.Plan == Plan.Nothing)
                    {
                        Problem(name + ": Plan が Nothing");
                    }

                    abilitiesByCard.TryGetValue(card.SupportCardId, out var abilityList);
                    var abilities = (abilityList ?? new SupportAbilityRow[0])
                        .Select(x => new SupportAbilityDefinition(x.Kind, x.Param, x.ValueAtLv1, x.ValueAtMax))
                        .ToArray();
                    var maxLevel = capRows.TryGetValue(Key((int)card.Rarity, ProduceRules.MaxLimitBreak), out var fullCap) ? fullCap.MaxLevel : 0;
                    result.Add(card.SupportCardId, new SupportCardDefinition(card.SupportCardId, card.Name, card.Rarity, card.Type, card.Plan, abilities, maxLevel));
                }

                return result;
            }

            private static Dictionary<long, int> BuildMaxLevels(Dictionary<long, SupportLevelCapRow> capRows)
            {
                var result = new Dictionary<long, int>();
                foreach (var cap in capRows.Values)
                {
                    result.Add(Key((int)cap.Rarity, cap.LimitBreak), cap.MaxLevel);
                }

                return result;
            }

            // V7
            private IReadOnlyList<int> BuildCommonDeck(Dictionary<int, CommonDeckCardRow> commonRows)
            {
                if (commonRows.Count < 1)
                {
                    Problem("CommonDeckCard: 1 行もない");
                }

                var ordered = commonRows.Values.OrderBy(x => x.Order).ToArray();
                foreach (var row in ordered)
                {
                    if (!cardIds.Contains(row.CardId))
                    {
                        Problem("CommonDeckCard " + row.Order + ": CardId " + row.CardId + " がカードにない");
                    }
                }

                return ordered.Select(x => x.CardId).ToArray();
            }

            // V8
            private Dictionary<Rarity, int> BuildOfferWeights(Dictionary<Rarity, OfferRarityWeightRow> weightRows)
            {
                var result = new Dictionary<Rarity, int>();
                foreach (var rarity in new[] { Rarity.N, Rarity.R, Rarity.SR, Rarity.SSR })
                {
                    if (!weightRows.TryGetValue(rarity, out var row))
                    {
                        Problem("OfferRarityWeight " + rarity + ": 行がない");
                        continue;
                    }

                    if (row.Weight < 1)
                    {
                        Problem("OfferRarityWeight " + rarity + ": Weight が 1 未満");
                    }

                    result.Add(rarity, row.Weight);
                }

                return result;
            }

            // V10 の ExamRankReward
            private Dictionary<long, int> BuildRewards(Dictionary<long, ExamRankRewardRow> rewardRows)
            {
                var result = new Dictionary<long, int>();
                foreach (var kind in new[] { ExamKind.Mid, ExamKind.Final })
                {
                    for (var rank = 1; rank <= 3; rank++)
                    {
                        if (!rewardRows.ContainsKey(Key((int)kind, rank)))
                        {
                            Problem("ExamRankReward " + kind + "/" + rank + ": 行がない");
                        }
                    }
                }

                foreach (var row in rewardRows.Values)
                {
                    if (row.ParamAll < 0)
                    {
                        Problem("ExamRankReward " + row.ExamKind + "/" + row.Rank + ": ParamAll が負");
                    }

                    result.Add(Key((int)row.ExamKind, row.Rank), row.ParamAll);
                }

                return result;
            }

            // V9 の ScoreBonusSetting
            private ScoreBonusSettingRow CheckScoreBonus()
            {
                if (rows.ScoreBonusSettings.Count != 1)
                {
                    Problem("ScoreBonusSetting: ちょうど 1 行でなければならない");
                    return default;
                }

                var row = rows.ScoreBonusSettings[0];
                if (row.APerThousand < 0 || row.BFirst < 0 || row.BSecond < 0 || row.BThird < 0)
                {
                    Problem("ScoreBonusSetting: 値が負");
                }

                return row;
            }

            // V9 の EvaluationScoreBand
            private IReadOnlyList<EvaluationScoreBandRow> BuildBands(Dictionary<int, EvaluationScoreBandRow> bandRows)
            {
                var ordered = bandRows.Values.OrderBy(x => x.Order).ToArray();
                if (ordered.Length < 1)
                {
                    Problem("EvaluationScoreBand: 1 行もない");
                    return ordered;
                }

                if (ordered[0].MinScore != 0)
                {
                    Problem("EvaluationScoreBand: 先頭の MinScore が 0 でない");
                }

                for (var i = 0; i < ordered.Length; i++)
                {
                    if (i > 0 && ordered[i].MinScore <= ordered[i - 1].MinScore)
                    {
                        Problem("EvaluationScoreBand " + ordered[i].Order + ": MinScore が増加していない");
                    }

                    if (ordered[i].ScalePerThousand < 0 || ordered[i].Fixed < 0)
                    {
                        Problem("EvaluationScoreBand " + ordered[i].Order + ": ScalePerThousand か Fixed が負");
                    }
                }

                return ordered;
            }

            // V10 の EvaluationRank
            private IReadOnlyList<EvaluationRankRow> BuildRanks(Dictionary<ProduceRank, EvaluationRankRow> rankRows)
            {
                var ordered = new List<EvaluationRankRow>();
                for (var r = (int)ProduceRank.F; r < (int)ProduceRank.F + RankCount; r++)
                {
                    var rank = (ProduceRank)r;
                    if (!rankRows.TryGetValue(rank, out var row))
                    {
                        Problem("EvaluationRank " + rank + ": 行がない");
                        continue;
                    }

                    if (rank == ProduceRank.F && row.MinValue != 0)
                    {
                        Problem("EvaluationRank F: MinValue が 0 でない");
                    }

                    if (ordered.Count > 0 && row.MinValue <= ordered[ordered.Count - 1].MinValue)
                    {
                        Problem("EvaluationRank " + rank + ": MinValue が増加していない");
                    }

                    ordered.Add(row);
                }

                return ordered;
            }
        }
    }
}

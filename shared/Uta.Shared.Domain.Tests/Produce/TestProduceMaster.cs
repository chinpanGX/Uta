using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Tests.Lesson;

namespace Uta.Shared.Domain.Tests.Produce
{
    /// <summary>masterdata/csv/ の CSV を読んで <see cref="ProduceMasterRows"/> を作る、テスト用の読み込み。</summary>
    public static class TestProduceMaster
    {
        // 先頭の 4 行（表の名前・出典・型・列名）は読み飛ばす
        private const int HeaderLines = 4;

        public static ProduceMasterRows LoadRows()
        {
            var dir = FindCsvDirectory();
            return new ProduceMasterRows
            {
                Scenarios = Read(dir, "scenario", c => new ScenarioRow(
                    Int(c, 0), c[1], Enum<Difficulty>(c, 2), Int(c, 3), Int(c, 4), Int(c, 5))),
                ScenarioWeeks = Read(dir, "scenarioWeek", c => new ScenarioWeekRow(
                    Int(c, 0), Int(c, 1), Enum<WeekKind>(c, 2), Int(c, 3), Int(c, 4), Int(c, 5), Int(c, 6), Int(c, 7), Int(c, 8))),
                WeekActionSlots = Read(dir, "weekActionSlot", c => new WeekActionSlotRow(
                    Int(c, 0), Int(c, 1), Int(c, 2), Enum<ActionKind>(c, 3))),
                ClassOptions = Read(dir, "classOption", c => new ClassOptionRow(
                    Int(c, 0), Int(c, 1), Int(c, 2), Int(c, 3), Int(c, 4), Bool(c, 5))),
                ExamPatterns = Read(dir, "examPattern", c => new ExamPatternRow(
                    Int(c, 0), Int(c, 1), Int(c, 2), Int(c, 3), Int(c, 4), Int(c, 5), Int(c, 6), Int(c, 7), Int(c, 8), Int(c, 9),
                    Int(c, 10), Int(c, 11), Int(c, 12), Int(c, 13))),
                ExamRankRewards = Read(dir, "examRankReward", c => new ExamRankRewardRow(
                    Enum<ExamKind>(c, 0), Int(c, 1), Int(c, 2))),
                ScoreBonusSettings = Read(dir, "scoreBonusSetting", c => new ScoreBonusSettingRow(
                    Int(c, 0), Int(c, 1), Int(c, 2), Int(c, 3), Int(c, 4))),
                EvaluationScoreBands = Read(dir, "evaluationScoreBand", c => new EvaluationScoreBandRow(
                    Int(c, 0), Int(c, 1), Int(c, 2), Int(c, 3))),
                EvaluationRanks = Read(dir, "evaluationRank", c => new EvaluationRankRow(
                    Enum<ProduceRank>(c, 0), Int(c, 1))),
                OfferRarityWeights = Read(dir, "offerRarityWeight", c => new OfferRarityWeightRow(
                    Enum<Rarity>(c, 0), Int(c, 1))),
                Idols = Read(dir, "idol", c => new IdolRow(
                    Int(c, 0), c[1], Enum<Rarity>(c, 2), Enum<Plan>(c, 3), Enum<RecommendKind>(c, 4), Int(c, 5),
                    Int(c, 6), Int(c, 7), Int(c, 8), Int(c, 9), Int(c, 10), Int(c, 11), Int(c, 12),
                    Enum<ParamKind>(c, 13), Enum<ParamKind>(c, 14), Enum<ParamKind>(c, 15), Int(c, 16))),
                IdolRecommendDecks = Read(dir, "idolRecommendDeck", c => new IdolRecommendDeckRow(
                    Enum<RecommendKind>(c, 0), Int(c, 1), Int(c, 2))),
                SupportCards = Read(dir, "supportCard", c => new SupportCardRow(
                    Int(c, 0), c[1], Enum<Rarity>(c, 2), Enum<SupportType>(c, 3), Enum<Plan>(c, 4))),
                SupportAbilities = Read(dir, "supportAbility", c => new SupportAbilityRow(
                    Int(c, 0), Int(c, 1), Enum<SupportAbilityKind>(c, 2), Enum<ParamKind>(c, 3), Int(c, 4), Int(c, 5))),
                SupportLevelCaps = Read(dir, "supportLevelCap", c => new SupportLevelCapRow(
                    Enum<Rarity>(c, 0), Int(c, 1), Int(c, 2))),
                CommonDeckCards = Read(dir, "commonDeckCard", c => new CommonDeckCardRow(
                    Int(c, 0), Int(c, 1))),
            };
        }

        public static ProduceCatalog Create(ICardCatalog cards = null)
        {
            return ProduceCatalog.Create(LoadRows(), cards ?? new TestCardCatalog());
        }

        public static ProduceCatalog Create(ProduceMasterRows rows, ICardCatalog cards = null)
        {
            return ProduceCatalog.Create(rows, cards ?? new TestCardCatalog());
        }

        private static string FindCsvDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "masterdata", "csv");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("masterdata/csv が見つかりません: " + AppContext.BaseDirectory);
        }

        private static IReadOnlyList<T> Read<T>(string dir, string table, Func<string[], T> map)
        {
            var text = File.ReadAllText(Path.Combine(dir, table + ".csv"), new System.Text.UTF8Encoding(false));
            var result = new List<T>();
            var lines = text.Split('\n');
            for (var i = HeaderLines; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                result.Add(map(line.Split(',')));
            }

            return result;
        }

        private static string Cell(string[] cells, int index)
        {
            return index < cells.Length ? cells[index] : string.Empty;
        }

        private static int Int(string[] cells, int index)
        {
            var value = Cell(cells, index);
            return value.Length == 0 ? 0 : int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static bool Bool(string[] cells, int index)
        {
            return string.Equals(Cell(cells, index), "TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private static T Enum<T>(string[] cells, int index)
            where T : struct
        {
            var value = Cell(cells, index);
            return value.Length == 0 ? default : System.Enum.Parse<T>(value);
        }
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uta.Client.Application.Support;
using Uta.Client.Domain.Player;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Presentation.Common;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using ZLinq;

namespace Uta.Client.Presentation.Owned
{
    /// <summary>サポートカードの詳細の表示の値と文字を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class SupportDetailDtoFactory
    {
        private static readonly string[] StepNames = { "+1", "+5", "最大" };

        private readonly IReadOnlyPlayerRepository playerRepository;
        private readonly IReadOnlySupportCardRepository supportCardRepository;
        private readonly Uta.MasterData.MemoryDatabase memoryDatabase;
        private readonly SupportLevelPlanner supportLevelPlanner;

        public SupportDetailDtoFactory(IReadOnlyPlayerRepository playerRepository, IReadOnlySupportCardRepository supportCardRepository,
            Uta.MasterData.MemoryDatabase memoryDatabase, SupportLevelPlanner supportLevelPlanner)
        {
            this.playerRepository = playerRepository;
            this.supportCardRepository = supportCardRepository;
            this.memoryDatabase = memoryDatabase;
            this.supportLevelPlanner = supportLevelPlanner;
        }

        public SupportDetailViewDto Create(string supportCardUniqueId)
        {
            var card = supportCardRepository.Find(supportCardUniqueId);
            if (card == null)
            {
                var gone = new List<SupportLevelStepViewDto>();
                foreach (var name in StepNames)
                {
                    gone.Add(new SupportLevelStepViewDto(name, false, 0, null));
                }

                return new SupportDetailViewDto(false, 0, new[] { "このカードは所持していません。" }, new string[0], "", gone, "上限解放", false, false);
            }

            var player = playerRepository.Find();
            var sp = player == null ? 0 : player.SupportPoints;
            var gold = player == null ? 0 : player.Gold;
            var info = memoryDatabase.SupportCardTable.FindBySupportCardId(card.MasterId);
            var maxLevel = supportLevelPlanner.GetMaxLevel(card.MasterId, card.LimitBreak);
            var maxBreak = supportLevelPlanner.GetMaxLimitBreak(card.MasterId);
            var fullMaxLevel = supportLevelPlanner.GetMaxLevel(card.MasterId, maxBreak);

            var infoLines = new[]
            {
                info.Name,
                DisplayText.RarityName((Rarity)(int)info.Rarity) + "　" + DisplayText.SupportTypeName((SupportType)(int)info.Type) + "　" + DisplayText.PlanName((Plan)(int)info.Plan),
                "Lv " + card.Level + " / " + maxLevel,
                "上限解放 " + card.LimitBreak + " / " + maxBreak,
            };

            var abilityLines = new List<string>();
            var abilities = memoryDatabase.SupportAbilityTable.All
                .Where(a => a.SupportCardId == card.MasterId)
                .OrderBy(a => a.Order)
                .ToArray();
            foreach (var a in abilities)
            {
                var value = ProduceFormulas.AbilityValue(a.ValueAtLv1, a.ValueAtMax, card.Level, fullMaxLevel);
                abilityLines.Add(a.Order + " " + DisplayText.Ability((SupportAbilityKind)(int)a.Kind, (ParamKind)(int)a.Param, value));
            }

            if (abilityLines.Count == 0)
            {
                abilityLines.Add("なし");
            }

            var steps = new List<SupportLevelStepViewDto>();
            for (var i = 0; i < StepNames.Length; i++)
            {
                steps.Add(CreateStep(card, StepNames[i], (LevelUpStepKind)i, sp, gold, maxLevel));
            }

            var limitBreakLabel = card.LimitBreak >= maxBreak ? "上限解放（最大）" : "上限解放（素材を選ぶ）";
            var hasMaterial = supportCardRepository.FindAll().Any(c => c.MasterId == card.MasterId && c.UniqueId != card.UniqueId);
            return new SupportDetailViewDto(true, card.MasterId, infoLines, abilityLines, "強化pt " + N0(sp) + "　ゴールド " + N0(gold),
                steps, limitBreakLabel, card.LimitBreak < maxBreak, hasMaterial);
        }

        /// <summary>上限解放の確認の文。カードか素材が所持に無ければ null。</summary>
        public string CreateLimitBreakConfirmText(string supportCardUniqueId, string materialUniqueId)
        {
            var card = supportCardRepository.Find(supportCardUniqueId);
            var material = supportCardRepository.Find(materialUniqueId);
            if (card == null || material == null)
            {
                return null;
            }

            var info = memoryDatabase.SupportCardTable.FindBySupportCardId(card.MasterId);
            return "Lv " + material.Level + " の「" + info.Name + "」を素材にして上限解放します（" + card.LimitBreak + " → " + (card.LimitBreak + 1)
                + "）。\n素材は無くなります。強化に使った強化pt・ゴールドは戻りません。";
        }

        private SupportLevelStepViewDto CreateStep(SupportCardEntity card, string name, LevelUpStepKind step, int sp, int gold, int maxLevel)
        {
            var target = supportLevelPlanner.PlanTarget(card, sp, gold, step);
            if (target > card.Level)
            {
                var cost = supportLevelPlanner.GetLevelUpCost(card.MasterId, card.Level, target);
                var label = name + " → Lv " + target + "（強化pt " + N0(cost.SupportPoints) + " / G " + N0(cost.Gold) + "）";
                var confirmText = "Lv " + card.Level + " → Lv " + target + " に上げます。\n強化pt " + N0(cost.SupportPoints) + "・ゴールド " + N0(cost.Gold) + " を使います。";
                return new SupportLevelStepViewDto(label, true, target, confirmText);
            }

            if (card.Level >= maxLevel)
            {
                return new SupportLevelStepViewDto(name + "（上限）", false, card.Level, null);
            }

            return new SupportLevelStepViewDto("最大（足りません）", false, card.Level, null);
        }

        private static string N0(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}

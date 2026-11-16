using System;
using System.Collections.Generic;
using Uta.Client.Application.Produce;
using Uta.Client.Domain.SupportCard;
using Uta.Client.Presentation.Common;
using Uta.MasterData;
using SharedPlan = Uta.Shared.Domain.Lesson.Plan;
using SharedRarity = Uta.Shared.Domain.Lesson.Rarity;

namespace Uta.Client.Presentation.Formation
{
    /// <summary>編成のページの表示の値と文字を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class FormationDtoFactory
    {
        private readonly IReadOnlySupportCardRepository supportCardRepository;
        private readonly ScenarioResolver scenarioResolver;
        private readonly MemoryDatabase memoryDatabase;

        public FormationDtoFactory(IReadOnlySupportCardRepository supportCardRepository, ScenarioResolver scenarioResolver, MemoryDatabase memoryDatabase)
        {
            this.supportCardRepository = supportCardRepository;
            this.scenarioResolver = scenarioResolver;
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>
        /// idolId が 0 なら育成アイドルは未選択。slotUids は枠ごとの所持ID（null は空）。所持に無い uid の枠は空として出す。
        /// slotUids が null なら ArgumentNullException。
        /// </summary>
        public FormationViewDto Create(int idolId, IReadOnlyList<string> slotUids, int scenarioIndex)
        {
            if (slotUids == null)
            {
                throw new ArgumentNullException(nameof(slotUids));
            }

            var idolLabel = "育成アイドル: 未選択";
            if (idolId != 0)
            {
                var idol = memoryDatabase.IdolTable.FindByIdolId(idolId);
                idolLabel = "育成アイドル: " + idol.Name + " [" + DisplayText.RarityName((SharedRarity)(int)idol.Rarity) + "] " + DisplayText.PlanName((SharedPlan)(int)idol.Plan);
            }

            var slots = new List<FormationSlotRowViewDto>();
            for (var i = 0; i < slotUids.Count; i++)
            {
                var card = slotUids[i] == null ? null : supportCardRepository.Find(slotUids[i]);
                if (card == null)
                {
                    slots.Add(new FormationSlotRowViewDto("枠" + (i + 1) + ": （空）", false));
                    continue;
                }

                var info = memoryDatabase.SupportCardTable.FindBySupportCardId(card.MasterId);
                slots.Add(new FormationSlotRowViewDto("枠" + (i + 1) + ": " + info.Name + " [" + DisplayText.RarityName((SharedRarity)(int)info.Rarity) + "] Lv " + card.Level, true));
            }

            var scenarioLabel = "シナリオ: なし";
            if (scenarioResolver.Count > 0)
            {
                scenarioLabel = "シナリオ: " + scenarioResolver.NameAt(scenarioIndex);
            }

            return new FormationViewDto(idolLabel, slots, scenarioLabel, scenarioResolver.Count >= 2, idolId != 0 && scenarioResolver.Count > 0);
        }
    }
}

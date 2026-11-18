using System;
using System.Collections.Generic;
using Uta.Client.Domain.SupportCard;
using Uta.MasterData;
using Uta.Shared.Domain.Produce;
using SharedPlan = Uta.Shared.Domain.Lesson.Plan;

namespace Uta.Client.Application.Produce
{
    /// <summary>編成の枠のうち、所持に無い・育成アイドルのプランに合わないサポートカードを外す。</summary>
    public sealed class FormationNormalizer
    {
        private readonly IReadOnlySupportCardRepository supportCardRepository;
        private readonly MemoryDatabase memoryDatabase;

        public FormationNormalizer(IReadOnlySupportCardRepository supportCardRepository, MemoryDatabase memoryDatabase)
        {
            this.supportCardRepository = supportCardRepository;
            this.memoryDatabase = memoryDatabase;
        }

        /// <summary>
        /// slotUids を順に見て、所持に無い uid と、idolId != 0 でプランが合わない uid を null にした新しい配列を返す。
        /// removed は null にした数（元から null の枠は数えない）。slotUids が null なら ArgumentNullException。
        /// </summary>
        public string[] RemoveIncompatible(int idolId, IReadOnlyList<string> slotUids, out int removed)
        {
            if (slotUids == null)
            {
                throw new ArgumentNullException(nameof(slotUids));
            }

            var idolPlan = SharedPlan.Nothing;
            if (idolId != 0)
            {
                idolPlan = (SharedPlan)(int)memoryDatabase.IdolTable.FindByIdolId(idolId).Plan;
            }

            var result = new string[slotUids.Count];
            removed = 0;
            for (var i = 0; i < slotUids.Count; i++)
            {
                var uid = slotUids[i];
                if (uid == null)
                {
                    continue;
                }

                var card = supportCardRepository.Find(uid);
                var keep = card != null;
                if (keep && idolId != 0)
                {
                    var cardPlan = (SharedPlan)(int)memoryDatabase.SupportCardTable.FindBySupportCardId(card.MasterId).Plan;
                    keep = FormationValidator.IsPlanCompatible(idolPlan, cardPlan);
                }

                if (keep)
                {
                    result[i] = uid;
                }
                else
                {
                    removed++;
                }
            }

            return result;
        }
    }
}

using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>編成の検証（idol-support.md 2.1）。</summary>
    public static class FormationValidator
    {
        /// <summary>
        /// 検証の 5・6。1〜4（所持の解決）と 7 はサーバー。
        /// idolId・カード ID が無ければ KeyNotFoundException。0 枚は Ok。
        /// </summary>
        public static FormationCheck Validate(ProduceCatalog catalog, int idolId, IReadOnlyList<int> supportCardIds)
        {
            var idol = catalog.GetIdol(idolId);
            var seen = new HashSet<int>();
            var duplicate = false;
            for (var i = 0; i < supportCardIds.Count; i++)
            {
                // 無いカード ID は先に KeyNotFoundException にする
                catalog.GetSupportCard(supportCardIds[i]);
                if (!seen.Add(supportCardIds[i]))
                {
                    duplicate = true;
                }
            }

            if (duplicate)
            {
                return FormationCheck.DuplicateCardId;
            }

            for (var i = 0; i < supportCardIds.Count; i++)
            {
                var plan = catalog.GetSupportCard(supportCardIds[i]).Plan;
                if (plan != Plan.Free && plan != idol.Plan)
                {
                    return FormationCheck.PlanMismatch;
                }
            }

            return FormationCheck.Ok;
        }
    }
}

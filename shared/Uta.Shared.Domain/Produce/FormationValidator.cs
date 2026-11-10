using System.Collections.Generic;
using Uta.Shared.Domain.Lesson;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>編成の検証（idol-support.md 2.1）。</summary>
    public static class FormationValidator
    {
        /// <summary>
        /// サポートカードのプランが育成アイドルのプランに合うか（idol-support.md 2.1 の 6）。
        /// supportPlan が Plan.Free か、idolPlan と同じなら true。それ以外（Plan.Nothing を含む）は false。
        /// クライアントの所持一覧・編成の表示と、Validate の両方がこれを使う（規則を 1 か所に置く）。
        /// </summary>
        public static bool IsPlanCompatible(Plan idolPlan, Plan supportPlan)
        {
            return supportPlan == Plan.Free || supportPlan == idolPlan;
        }

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
                if (!IsPlanCompatible(idol.Plan, plan))
                {
                    return FormationCheck.PlanMismatch;
                }
            }

            return FormationCheck.Ok;
        }
    }
}

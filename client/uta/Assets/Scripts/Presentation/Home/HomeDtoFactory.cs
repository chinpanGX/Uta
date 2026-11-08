using System.Globalization;
using Uta.Client.Application.Idol;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Player;
using Uta.MasterData;

namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームの表示の値を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class HomeDtoFactory
    {
        private readonly IReadOnlyPlayerRepository players;
        private readonly FeaturedIdolPicker picker;
        private readonly DiscardNoticeState notice;   // 読むだけ
        private readonly MemoryDatabase db;

        public HomeDtoFactory(IReadOnlyPlayerRepository players, FeaturedIdolPicker picker, DiscardNoticeState notice, MemoryDatabase db)
        {
            this.players = players;
            this.picker = picker;
            this.notice = notice;
            this.db = db;
        }

        /// <summary>プレイヤーがまだ無ければ null。</summary>
        public HomeViewDto Create()
        {
            var p = players.Find();
            if (p == null)
            {
                return null;
            }

            var profileText = "プレイヤーLv " + p.Level + "　ガチャ通貨 " + p.GachaCurrency.ToString("N0", CultureInfo.InvariantCulture);
            var resumeVisible = p.HasActiveProduce;
            var primaryLabel = resumeVisible ? "再開" : "プロデュース";
            var showDiscardNotice = p.DiscardedProduce && !notice.Shown;
            var stageIdolId = picker.Pick();
            string stageIdolName = null;
            if (stageIdolId != 0)
            {
                stageIdolName = db.IdolTable.FindByIdolId(stageIdolId).Name;
            }

            return new HomeViewDto(profileText, resumeVisible, primaryLabel, showDiscardNotice, stageIdolId, stageIdolName);
        }
    }
}

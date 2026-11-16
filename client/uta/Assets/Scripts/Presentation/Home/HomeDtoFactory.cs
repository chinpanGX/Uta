using System.Globalization;
using Uta.Client.Application.Idol;
using Uta.Client.Application.Player;
using Uta.Client.Domain.Player;
using Uta.MasterData;
using Uta.Shared.Domain.Player;

namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームの表示の値を作る。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。</summary>
    public sealed class HomeDtoFactory
    {
        private readonly IReadOnlyPlayerRepository playerRepository;
        private readonly FeaturedIdolPicker featuredIdolPicker;
        private readonly DiscardNoticeState discardNoticeState;   // 読むだけ
        private readonly MemoryDatabase memoryDatabase;
        private readonly PlayerLevelCatalog playerLevelCatalog;

        public HomeDtoFactory(IReadOnlyPlayerRepository playerRepository, FeaturedIdolPicker featuredIdolPicker, DiscardNoticeState discardNoticeState, MemoryDatabase memoryDatabase, PlayerLevelCatalog playerLevelCatalog)
        {
            this.playerRepository = playerRepository;
            this.featuredIdolPicker = featuredIdolPicker;
            this.discardNoticeState = discardNoticeState;
            this.memoryDatabase = memoryDatabase;
            this.playerLevelCatalog = playerLevelCatalog;
        }

        /// <summary>プレイヤーがまだ無ければ null。</summary>
        public HomeViewDto Create()
        {
            var p = playerRepository.Find();
            if (p == null)
            {
                return null;
            }

            var profileText = "プレイヤーLv " + playerLevelCatalog.GetLevel(p.Exp) + "　ガチャ通貨 " + p.GachaCurrency.ToString("N0", CultureInfo.InvariantCulture);
            var resumeVisible = p.HasActiveProduce;
            var primaryLabel = resumeVisible ? "再開" : "プロデュース";
            var showDiscardNotice = p.DiscardedProduce && !discardNoticeState.Shown;
            var stageIdolId = featuredIdolPicker.Pick();
            string stageIdolName = null;
            if (stageIdolId != 0)
            {
                stageIdolName = memoryDatabase.IdolTable.FindByIdolId(stageIdolId).Name;
            }

            return new HomeViewDto(profileText, resumeVisible, primaryLabel, showDiscardNotice, stageIdolId, stageIdolName);
        }
    }
}

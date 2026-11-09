namespace Uta.Client.Presentation.Home
{
    /// <summary>ホームの表示の値。stageIdolId が 0 なら所持なし（stageIdolName は null）。</summary>
    public sealed class HomeViewDto
    {
        public HomeViewDto(string profileText, bool resumeVisible, string primaryLabel, bool showDiscardNotice, int stageIdolId, string stageIdolName)
        {
            ProfileText = profileText;
            ResumeVisible = resumeVisible;
            PrimaryLabel = primaryLabel;
            ShowDiscardNotice = showDiscardNotice;
            StageIdolId = stageIdolId;
            StageIdolName = stageIdolName;
        }

        public string ProfileText { get; }
        public bool ResumeVisible { get; }
        public string PrimaryLabel { get; }

        /// <summary>破棄の知らせのダイアログを出すか。</summary>
        public bool ShowDiscardNotice { get; }

        public int StageIdolId { get; }
        public string StageIdolName { get; }
    }
}

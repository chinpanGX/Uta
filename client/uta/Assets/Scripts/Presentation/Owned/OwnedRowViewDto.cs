namespace Uta.Client.Presentation.Owned
{
    public sealed class OwnedRowViewDto
    {
        public OwnedRowViewDto(int idolId, string supportCardUid, string title, string detail, bool enabled)
        {
            IdolId = idolId;
            SupportCardUid = supportCardUid;
            Title = title;
            Detail = detail;
            Enabled = enabled;
        }

        /// <summary>育成アイドルの行: IdolId。サポートカードの行: 0。</summary>
        public int IdolId { get; }

        /// <summary>サポートカードの行: 所持ID。育成アイドルの行: null。</summary>
        public string SupportCardUid { get; }

        public string Title { get; }
        public string Detail { get; }
        public bool Enabled { get; }
    }
}

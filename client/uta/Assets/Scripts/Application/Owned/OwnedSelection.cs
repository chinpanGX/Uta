namespace Uta.Client.Application.Owned
{
    /// <summary>選択モードの結果。</summary>
    public sealed class OwnedSelection
    {
        private static readonly OwnedSelection CancelledInstance = new OwnedSelection(true, 0, null);

        private OwnedSelection(bool isCancelled, int idolId, string supportCardUid)
        {
            IsCancelled = isCancelled;
            IdolId = idolId;
            SupportCardUid = supportCardUid;
        }

        /// <summary>1 つのインスタンスを使い回す。</summary>
        public static OwnedSelection Cancelled
        {
            get { return CancelledInstance; }
        }

        public static OwnedSelection ForIdol(int idolId)
        {
            return new OwnedSelection(false, idolId, null);
        }

        /// <summary>素材の選択もこれ。</summary>
        public static OwnedSelection ForSupportCard(string supportCardUid)
        {
            return new OwnedSelection(false, 0, supportCardUid);
        }

        public bool IsCancelled { get; }

        /// <summary>ForIdol 以外は 0。</summary>
        public int IdolId { get; }

        /// <summary>ForSupportCard 以外は null。</summary>
        public string SupportCardUid { get; }
    }
}

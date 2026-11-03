namespace Uta.Client.Application.Player
{
    public sealed class OwnedSupportCard
    {
        public OwnedSupportCard(string supportCardUid, int cardId, int level, int limitBreak)
        {
            SupportCardUid = supportCardUid;
            CardId = cardId;
            Level = level;
            LimitBreak = limitBreak;
        }

        /// <summary>ULID 26 文字。</summary>
        public string SupportCardUid { get; }
        public int CardId { get; }
        public int Level { get; }
        public int LimitBreak { get; }
    }
}

namespace Uta.Client.Presentation.Owned
{
    public sealed class SupportDetailArgs
    {
        public SupportDetailArgs(string supportCardUniqueId)
        {
            SupportCardUniqueId = supportCardUniqueId;
        }

        public string SupportCardUniqueId { get; }
    }
}

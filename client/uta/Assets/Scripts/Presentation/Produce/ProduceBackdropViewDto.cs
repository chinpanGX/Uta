namespace Uta.Client.Presentation.Produce
{
    /// <summary>プロデュースの背景のページの表示の値。</summary>
    public sealed class ProduceBackdropViewDto
    {
        public ProduceBackdropViewDto(string header)
        {
            Header = header;
        }

        public string Header { get; }
    }
}

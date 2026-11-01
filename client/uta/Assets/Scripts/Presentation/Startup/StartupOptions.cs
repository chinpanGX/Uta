namespace Uta.Client.Presentation.Startup
{
    public sealed class StartupOptions
    {
        public StartupOptions(bool quitAfterCheck)
        {
            QuitAfterCheck = quitAfterCheck;
        }

        public bool QuitAfterCheck { get; }
    }
}

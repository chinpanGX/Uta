using System;
using Uta.Client.Application.Player;

namespace Uta.Client.Presentation.Title
{
    /// <summary>ログの行の文（TitlePresenter と TitleDiagnostics が同じ文を出す）。</summary>
    public static class StartupLogs
    {
        public static string Device(bool registered)
        {
            return registered ? "device: registered" : "device: token issued";
        }

        public static string SignUp(EnterPlayerResult entered)
        {
            return "sign-up: " + (!entered.SignedUp ? "skipped" : entered.Created ? "created" : "already exists");
        }

        public static string SignIn(EnterPlayerResult e)
        {
            return "sign-in: ok level=" + e.Player.Level + " exp=" + e.Player.Exp + " gold=" + e.Player.Gold + " supportPoints=" + e.Player.SupportPoints + " gachaCurrency=" + e.Player.GachaCurrency + " idols=" + e.IdolCount + " supportCards=" + e.SupportCardCount + " hasActiveProduce=" + e.Player.HasActiveProduce + " discardedProduce=" + e.Player.DiscardedProduce;
        }

        public static string Failed(Exception e)
        {
            return "start: FAILED " + e.GetType().FullName + ": " + e.Message;
        }
    }
}

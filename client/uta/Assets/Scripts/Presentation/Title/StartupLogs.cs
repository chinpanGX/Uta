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

        public static string SignUp(EnterPlayerResult enterPlayerResult)
        {
            return "sign-up: " + (!enterPlayerResult.SignedUp ? "skipped" : enterPlayerResult.Created ? "created" : "already exists");
        }

        public static string SignIn(EnterPlayerResult enterPlayerResult)
        {
            return "sign-in: ok level=" + enterPlayerResult.Player.Level + " exp=" + enterPlayerResult.Player.Exp + " gold=" + enterPlayerResult.Player.Gold + " supportPoints=" + enterPlayerResult.Player.SupportPoints + " gachaCurrency=" + enterPlayerResult.Player.GachaCurrency + " idols=" + enterPlayerResult.IdolCount + " supportCards=" + enterPlayerResult.SupportCardCount + " hasActiveProduce=" + enterPlayerResult.Player.HasActiveProduce + " discardedProduce=" + enterPlayerResult.Player.DiscardedProduce;
        }

        public static string Failed(Exception exception)
        {
            return "start: FAILED " + exception.GetType().FullName + ": " + exception.Message;
        }
    }
}

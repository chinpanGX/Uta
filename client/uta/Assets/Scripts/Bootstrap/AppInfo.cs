using System;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    public static class AppInfo
    {
        public const int Version = 1;

        /// <summary>アカウントのファイルの鍵の元。Windows のプレイヤーでは Application.identifier が空なので、全プラットフォームでこの固定の値を使う。</summary>
        public const string AccountKeySource = "Uta.IdolMaster.Hommage";

        public static string Platform
        {
            get
            {
#if UNITY_EDITOR
                return AppVersion.Editor;
#elif UNITY_STANDALONE_WIN
                return AppVersion.Windows;
#elif UNITY_ANDROID
                return AppVersion.Android;
#elif UNITY_IOS
                return AppVersion.Ios;
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }
    }
}

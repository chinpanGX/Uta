using System;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    public static class AppInfo
    {
        public const int Version = 1;

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

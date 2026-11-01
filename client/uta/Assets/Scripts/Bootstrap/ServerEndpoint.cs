using System;
using System.Text;

namespace Uta.Client.Bootstrap
{
    public static class ServerEndpoint
    {
        private const string DefaultUrl = "http://localhost:5000";
        private const string UrlOption = "-uta-server-url";

        public static string Resolve(string[] commandLineArgs)
        {
            for (var i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (commandLineArgs[i] == UrlOption)
                {
                    return commandLineArgs[i + 1];
                }
            }

            return DefaultUrl;
        }

        public static string AccountFileName(string serverUrl)
        {
            var builder = new StringBuilder("account-");
            foreach (var c in serverUrl)
            {
                var isAlphanumeric = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                builder.Append(isAlphanumeric ? c : '_');
            }

            builder.Append(".dat");
            return builder.ToString();
        }
    }
}

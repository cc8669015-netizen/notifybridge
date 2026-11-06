using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace NotifyBridge
{
    /// <summary>
    /// Talks to your relay server for the "anywhere" push mode: registers this phone's WNS push
    /// channel under a short pairing code, so the Android app only needs to know that code.
    /// Requires RelayConfig.ServerUrl to point at a server running the code in Relay-Server/.
    /// </summary>
    public static class RelayClient
    {
        private static readonly HttpClient Http = new HttpClient();

        public static async Task<PushNotificationChannel> CreateChannelAsync()
        {
            return await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
        }

        /// <summary>Tells the relay: "notifications for code X go to this channel URI."</summary>
        public static async Task RegisterAsync(string pairingCode, string channelUri)
        {
            if (!RelayConfig.IsValid(RelayConfig.ServerUrl))
                throw new InvalidOperationException(
                    "Enter your relay server's address first, starting with https:// (see the README).");

            var body = new JsonObject
            {
                ["code"] = JsonValue.CreateStringValue(pairingCode),
                ["channelUri"] = JsonValue.CreateStringValue(channelUri)
            };

            string url = RelayConfig.ServerUrl + "/register";
            using (var content = new StringContent(body.Stringify(), Encoding.UTF8, "application/json"))
            using (var response = await Http.PostAsync(url, content))
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }

    /// <summary>
    /// Which relay server to use. The user types it into the Push tab and it is remembered on the
    /// phone, so anyone can point the app at their own server without rebuilding it.
    /// </summary>
    public static class RelayConfig
    {
        private const string SettingKey = "relayServerUrl";

        /// <summary>
        /// Optional built-in address used when the user hasn't entered one. Leave this empty in
        /// anything you share so people use their own server, not yours.
        /// </summary>
        public const string DefaultServerUrl = "";

        /// <summary>The address in use, cleaned up (no spaces, no trailing slash, https:// added if missing).</summary>
        public static string ServerUrl
        {
            get
            {
                string saved = ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
                return Normalize(string.IsNullOrWhiteSpace(saved) ? DefaultServerUrl : saved);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ApplicationData.Current.LocalSettings.Values.Remove(SettingKey);
                else
                    ApplicationData.Current.LocalSettings.Values[SettingKey] = value.Trim();
            }
        }

        public static string Normalize(string input)
        {
            string url = (input ?? string.Empty).Trim();
            if (url.Length == 0) return string.Empty;
            if (!url.Contains("://")) url = "https://" + url;
            return url.TrimEnd('/');
        }

        /// <summary>Only https is accepted: the Android app refuses plain http, and notification text shouldn't travel unencrypted.</summary>
        public static bool IsValid(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) && string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        }
    }
}

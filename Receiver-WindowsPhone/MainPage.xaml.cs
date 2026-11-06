using System;
using System.Collections.ObjectModel;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NotifyBridge
{
    public sealed partial class MainPage : Page
    {
        private const string PairingCodeKey = "pairingCode";
        private const string PushEnabledKey = "pushEnabled";

        private readonly BridgeServer _server = new BridgeServer();
        private readonly ObservableCollection<BridgeNotification> _notifications = new ObservableCollection<BridgeNotification>();
        private CoreDispatcher _dispatcher;
        private string _pairingCode;

        public MainPage()
        {
            InitializeComponent();
            _dispatcher = Window.Current.Dispatcher;

            NotificationList.ItemsSource = _notifications;
            _notifications.CollectionChanged += (s, e) => UpdateEmptyState();
            UpdateEmptyState();

            _server.NotificationArrived += Server_NotificationArrived;

            string ip = BridgeServer.GetLocalIPv4();
            if (ip == null)
            {
                NoWifiText.Visibility = Visibility.Visible;
                AddressText.Text = string.Empty;
            }
            else
            {
                AddressText.Text = ip + ":" + BridgeServer.Port;
            }

            _pairingCode = GetOrCreatePairingCode();
            PairingCodeText.Text = _pairingCode;
            ServerUrlBox.Text = RelayConfig.ServerUrl;
            // Shown so you can check it against the Package Family Name in Partner Center:
            // push only works when the two are identical.
            IdentityText.Text = "App identity: " + Windows.ApplicationModel.Package.Current.Id.FamilyName;

            Loaded += MainPage_Loaded;
        }

        // WNS push channels expire (about every 30 days) and the relay forgets codes on restart,
        // so if push was set up before, quietly register again every time the app opens.
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainPage_Loaded;
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (!(settings[PushEnabledKey] is bool enabled) || !enabled) return;
            if (!RelayConfig.IsValid(RelayConfig.ServerUrl)) return;

            try
            {
                var channel = await RelayClient.CreateChannelAsync();
                await RelayClient.RegisterAsync(_pairingCode, channel.Uri);
                PushStatusText.Text = "Push is active for code " + _pairingCode + ".";
            }
            catch (Exception ex)
            {
                PushStatusText.Text = "Couldn't refresh push registration: " + ex.Message;
            }
        }

        // ---------- Local mode ----------

        private async void ServerToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServerToggle.IsOn)
                {
                    await _server.StartAsync();
                    StatusText.Text = "Waiting for your Android phone to connect...";
                }
                else
                {
                    _server.Stop();
                    StatusText.Text = "Stopped.";
                }
            }
            catch (Exception ex)
            {
                ServerToggle.IsOn = false;
                StatusText.Text = "Couldn't start: " + ex.Message;
            }
        }

        private async void Server_NotificationArrived(object sender, NotificationArrivedEventArgs e)
        {
            // The listener runs on a background thread; touch the UI back on the dispatcher.
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _notifications.Insert(0, e.Notification);
                StatusText.Text = "Connected. Last received " + e.Notification.TimeText;
                ShowToast(e.Notification);
            });
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _notifications.Clear();
        }

        private void UpdateEmptyState()
        {
            EmptyText.Visibility = _notifications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Push mode ----------

        private string GetOrCreatePairingCode()
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            string code = settings[PairingCodeKey] as string;
            if (string.IsNullOrEmpty(code))
            {
                // A 6-digit code, easy to type on a phone. Cryptographic RNG so it isn't guessable.
                uint n = Windows.Security.Cryptography.CryptographicBuffer.GenerateRandomNumber();
                code = (100000 + n % 900000).ToString();
                settings[PairingCodeKey] = code;
            }
            return code;
        }

        private void ServerUrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Remember whatever the user typed; RelayConfig cleans it up when it is used.
            RelayConfig.ServerUrl = ServerUrlBox.Text;
        }

        private async void PushSetup_Click(object sender, RoutedEventArgs e)
        {
            PushSetupButton.IsEnabled = false;
            PushStatusText.Text = "Requesting a push channel from Windows...";

            try
            {
                var channel = await RelayClient.CreateChannelAsync();
                PushStatusText.Text = "Got a channel. Registering with your relay server...";

                await RelayClient.RegisterAsync(_pairingCode, channel.Uri);
                ApplicationData.Current.LocalSettings.Values[PushEnabledKey] = true;
                PushStatusText.Text = "Registered. Notifications sent to code " + _pairingCode +
                                       " will now arrive here, even with this app closed.";
            }
            catch (InvalidOperationException ex)
            {
                PushStatusText.Text = ex.Message;
            }
            catch (Exception ex)
            {
                PushStatusText.Text = "Couldn't set up push: " + ex.Message +
                                       " Check the relay server address above and that your relay server is running.";
            }
            finally
            {
                PushSetupButton.IsEnabled = true;
            }
        }

        // ---------- Toasts (local mode; push-mode toasts are built by the relay server) ----------

        private static void ShowToast(BridgeNotification notification)
        {
            string xml =
                "<toast><visual><binding template='ToastGeneric'>" +
                "<text>" + Escape(notification.Heading) + "</text>" +
                "<text>" + Escape(notification.Title) + "</text>" +
                "<text>" + Escape(notification.Text) + "</text>" +
                "</binding></visual></toast>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(doc));
        }

        private static string Escape(string text)
        {
            return (text ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}

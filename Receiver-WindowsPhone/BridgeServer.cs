using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;

namespace NotifyBridge
{
    public class NotificationArrivedEventArgs : EventArgs
    {
        public NotificationArrivedEventArgs(BridgeNotification notification)
        {
            Notification = notification;
        }

        public BridgeNotification Notification { get; }
    }

    /// <summary>
    /// A small TCP server. The Android app opens a connection and sends one JSON object per
    /// line (newline-delimited), one per notification. There's no encryption or pairing check
    /// beyond a 6-digit code shown on screen, both devices must already trust the same Wi-Fi.
    /// </summary>
    public sealed class BridgeServer : IDisposable
    {
        public const int Port = 48651; // arbitrary, unlikely to collide with anything else

        private StreamSocketListener _listener;

        public bool IsRunning { get; private set; }
        public event EventHandler<NotificationArrivedEventArgs> NotificationArrived;
        public event EventHandler<string> ClientConnected; // passes the sender's address, for the log

        public async Task StartAsync()
        {
            if (IsRunning) return;

            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += Listener_ConnectionReceived;
            await _listener.BindServiceNameAsync(Port.ToString());
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            if (_listener != null)
            {
                _listener.ConnectionReceived -= Listener_ConnectionReceived;
                _listener.Dispose();
                _listener = null;
            }
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            string remote = args.Socket.Information.RemoteAddress?.DisplayName ?? "unknown";
            RaiseConnected(remote);

            // One phone can stay connected and keep sending lines; read until it disconnects.
            try
            {
                using (var reader = new StreamReader(args.Socket.InputStream.AsStreamForRead(), Encoding.UTF8))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.Trim().Length == 0) continue;
                        HandleLine(line);
                    }
                }
            }
            catch (Exception)
            {
                // Connection dropped. The phone's sender app reconnects on its own.
            }
        }

        private void HandleLine(string json)
        {
            BridgeNotification notification;
            try
            {
                notification = Json.Parse<BridgeNotification>(json);
            }
            catch (Exception)
            {
                return; // not a message we understand; ignore rather than crash the listener
            }

            if (notification == null) return;
            notification.ReceivedLocal = DateTime.Now;

            var handler = NotificationArrived;
            if (handler != null) handler(this, new NotificationArrivedEventArgs(notification));
        }

        private void RaiseConnected(string remote)
        {
            var handler = ClientConnected;
            if (handler != null) handler(this, remote);
        }

        /// <summary>The device's Wi-Fi IPv4 address, to show on screen for pairing. May be null if not on Wi-Fi.</summary>
        public static string GetLocalIPv4()
        {
            foreach (HostName name in NetworkInformation.GetHostNames())
            {
                if (name.Type == HostNameType.Ipv4 && name.IPInformation?.NetworkAdapter != null)
                {
                    return name.CanonicalName;
                }
            }
            return null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

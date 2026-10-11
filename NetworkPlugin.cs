using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using MacroDeck.Plugins;
using MacroDeck.Variables;

namespace MacroDeckNetworkPlugin
{
    public class NetworkPlugin : MacroDeckPlugin
    {
        private CancellationTokenSource _cts;

        public override void Enable()
        {
            base.Enable();
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorNetwork(_cts.Token));
        }

        private async Task MonitorNetwork(CancellationToken token)
        {
            long lastBytesReceived = 0;
            long lastBytesSent = 0;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
                try
                {
                    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up &&
                            (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                             nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                        {
                            var stats = nic.GetIPv4Statistics();
                            long download = stats.BytesReceived - lastBytesReceived;
                            long upload = stats.BytesSent - lastBytesSent;
                            
                            lastBytesReceived = stats.BytesReceived;
                            lastBytesSent = stats.BytesSent;

                            VariableManager.SetValue("netzwerk_download", FormatSpeed(download), this);
                            VariableManager.SetValue("netzwerk_upload", FormatSpeed(upload), this);
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        private string FormatSpeed(long bytesPerSec)
        {
            if (bytesPerSec >= 1024 * 1024)
                return $"{bytesPerSec / (1024.0 * 1024.0):F1} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024.0:F0} KB/s";
            return $"{bytesPerSec} B/s";
        }

        public override void Disable()
        {
            base.Disable();
            _cts?.Cancel();
        }
    }
}

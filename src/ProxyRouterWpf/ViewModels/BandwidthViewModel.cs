using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ProxyRouterWpf.Bandwidth;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Helpers;

namespace ProxyRouterWpf.ViewModels
{
    public partial class BandwidthViewModel : ObservableObject
    {
        readonly AppServices _svc;
        readonly DispatcherTimer _timer;

        public BandwidthViewModel(AppServices svc)
        {
            _svc = svc;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _timer.Tick += (_, _) => Tick();
        }

        [ObservableProperty] string downloadText = "0 B/s";
        [ObservableProperty] string uploadText = "0 B/s";
        [ObservableProperty] string statusText = "Đang chờ dữ liệu...";
        [ObservableProperty] long revision;

        public IReadOnlyList<NetworkBandwidthSnapshot> GetRecent() => _svc.BandwidthCache.GetRecent();

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        void Tick()
        {
            var cur = _svc.BandwidthCache.Current;
            if (cur != null)
            {
                DownloadText = BytesFormatter.FormatRate(cur.BytesReceivedPerSec);
                UploadText = BytesFormatter.FormatRate(cur.BytesSentPerSec);
                StatusText = "Cập nhật: " + cur.SampledAtUtc.ToLocalTime().ToString("HH:mm:ss");
            }
            Revision++;
        }
    }
}

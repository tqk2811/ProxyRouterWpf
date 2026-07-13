using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProxyRouterWpf.Bandwidth;
using ProxyRouterWpf.ViewModels;

namespace ProxyRouterWpf.Views
{
    public partial class BandwidthView : UserControl
    {
        BandwidthViewModel? _vm;

        public BandwidthView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += (_, _) => Detach();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Detach();
            _vm = DataContext as BandwidthViewModel;
            if (_vm != null)
                _vm.PropertyChanged += OnVmPropertyChanged;
        }

        void Detach()
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BandwidthViewModel.Revision))
                Draw();
        }

        void Chart_SizeChanged(object sender, SizeChangedEventArgs e) => Draw();

        void Draw()
        {
            if (_vm == null) return;
            var canvas = ChartCanvas;
            canvas.Children.Clear();

            double w = canvas.ActualWidth, h = canvas.ActualHeight;
            if (w < 4 || h < 4) return;

            var samples = _vm.GetRecent();
            if (samples.Count < 2) return;

            long max = 1;
            foreach (var s in samples)
            {
                if (s.BytesReceivedPerSec > max) max = s.BytesReceivedPerSec;
                if (s.BytesSentPerSec > max) max = s.BytesSentPerSec;
            }

            DrawSeries(samples, s => s.BytesReceivedPerSec, max, w, h, "Brush.Success");
            DrawSeries(samples, s => s.BytesSentPerSec, max, w, h, "Brush.Info");
        }

        void DrawSeries(IReadOnlyList<NetworkBandwidthSnapshot> samples, Func<NetworkBandwidthSnapshot, long> pick, long max, double w, double h, string brushKey)
        {
            int n = samples.Count;
            var baseColor = (TryFindResource(brushKey) as SolidColorBrush)?.Color ?? Colors.Teal;

            var line = new Polyline
            {
                Stroke = new SolidColorBrush(baseColor),
                StrokeThickness = 1.6,
                StrokeLineJoin = PenLineJoin.Round,
            };
            var fill = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(46, baseColor.R, baseColor.G, baseColor.B)),
            };

            var pts = new PointCollection(n);
            for (int i = 0; i < n; i++)
            {
                double x = n == 1 ? 0 : i / (double)(n - 1) * w;
                double y = h - pick(samples[i]) / (double)max * (h - 4) - 2;
                pts.Add(new Point(x, y));
            }
            line.Points = pts;

            var poly = new PointCollection(pts) { };
            poly.Insert(0, new Point(0, h));
            poly.Add(new Point(w, h));
            fill.Points = poly;

            ChartCanvas.Children.Add(fill);
            ChartCanvas.Children.Add(line);
        }
    }
}

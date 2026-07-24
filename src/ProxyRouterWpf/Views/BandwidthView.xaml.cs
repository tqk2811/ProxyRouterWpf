using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProxyRouterWpf.Bandwidth;
using ProxyRouterWpf.Helpers;
using ProxyRouterWpf.ViewModels;

namespace ProxyRouterWpf.Views
{
    public partial class BandwidthView : UserControl
    {
        // Chart margins (device-independent pixels) reserved for the axis labels.
        const double PadLeft = 64, PadRight = 8, PadTop = 6, PadBottom = 18;
        const int YGridCount = 4;
        const int XTickCount = 4;

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

            double plotW = w - PadLeft - PadRight;
            double plotH = h - PadTop - PadBottom;
            if (plotW < 4 || plotH < 4) return;

            var samples = _vm.GetRecent();
            if (samples.Count < 2) return;

            // The tallest sample defines the Y scale, so the peak always touches the top gridline.
            long max = 1;
            foreach (var s in samples)
            {
                if (s.BytesReceivedPerSec > max) max = s.BytesReceivedPerSec;
                if (s.BytesSentPerSec > max) max = s.BytesSentPerSec;
            }

            DrawAxes(samples, max, plotW, plotH);
            DrawSeries(samples, s => s.BytesReceivedPerSec, max, plotW, plotH, "Brush.Success");
            DrawSeries(samples, s => s.BytesSentPerSec, max, plotW, plotH, "Brush.Info");
        }

        void DrawAxes(IReadOnlyList<NetworkBandwidthSnapshot> samples, long max, double plotW, double plotH)
        {
            var gridBrush = TryFindResource("Brush.Surface.BorderBrush") as Brush ?? Brushes.Gray;
            var axisBrush = TryFindResource("Brush.Control.BorderBrush") as Brush ?? Brushes.Gray;

            // Y axis: horizontal gridlines + rate labels (top = peak, bottom = 0).
            for (int i = 0; i <= YGridCount; i++)
            {
                double frac = i / (double)YGridCount;
                double y = PadTop + plotH - frac * plotH;
                long value = (long)(max * frac);

                ChartCanvas.Children.Add(new Line
                {
                    X1 = PadLeft,
                    Y1 = y,
                    X2 = PadLeft + plotW,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1,
                });

                var label = MakeLabel(BytesFormatter.FormatRate(value));
                label.Width = PadLeft - 6;
                label.TextAlignment = TextAlignment.Right;
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 7);
                ChartCanvas.Children.Add(label);
            }

            // X axis: vertical time labels (right = newest, left = oldest).
            double spanSeconds = (samples[^1].SampledAtUtc - samples[0].SampledAtUtc).TotalSeconds;
            if (spanSeconds < 0) spanSeconds = 0;
            for (int j = 0; j <= XTickCount; j++)
            {
                double frac = j / (double)XTickCount;
                double x = PadLeft + frac * plotW;
                int secondsAgo = (int)Math.Round(spanSeconds * (1 - frac));
                string text = secondsAgo == 0 ? "0s" : $"-{secondsAgo}s";

                var label = MakeLabel(text);
                label.Width = 44;
                label.TextAlignment = TextAlignment.Center;
                double left = Math.Clamp(x - label.Width / 2, 0, PadLeft + plotW - label.Width);
                Canvas.SetLeft(label, left);
                Canvas.SetTop(label, PadTop + plotH + 3);
                ChartCanvas.Children.Add(label);
            }

            // Axis baselines (left + bottom) drawn on top of the gridlines.
            ChartCanvas.Children.Add(new Line
            {
                X1 = PadLeft,
                Y1 = PadTop,
                X2 = PadLeft,
                Y2 = PadTop + plotH,
                Stroke = axisBrush,
                StrokeThickness = 1,
            });
            ChartCanvas.Children.Add(new Line
            {
                X1 = PadLeft,
                Y1 = PadTop + plotH,
                X2 = PadLeft + plotW,
                Y2 = PadTop + plotH,
                Stroke = axisBrush,
                StrokeThickness = 1,
            });
        }

        TextBlock MakeLabel(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
            };
            tb.SetResourceReference(ForegroundProperty, "Brush.Text.Secondary");
            return tb;
        }

        void DrawSeries(IReadOnlyList<NetworkBandwidthSnapshot> samples, Func<NetworkBandwidthSnapshot, long> pick, long max, double plotW, double plotH, string brushKey)
        {
            int n = samples.Count;
            var baseColor = (TryFindResource(brushKey) as SolidColorBrush)?.Color ?? Colors.Teal;
            double bottom = PadTop + plotH;

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
                double x = PadLeft + (n == 1 ? 0 : i / (double)(n - 1) * plotW);
                double y = PadTop + plotH - pick(samples[i]) / (double)max * plotH;
                pts.Add(new Point(x, y));
            }
            line.Points = pts;

            var poly = new PointCollection(pts) { };
            poly.Insert(0, new Point(PadLeft, bottom));
            poly.Add(new Point(PadLeft + plotW, bottom));
            fill.Points = poly;

            ChartCanvas.Children.Add(fill);
            ChartCanvas.Children.Add(line);
        }
    }
}

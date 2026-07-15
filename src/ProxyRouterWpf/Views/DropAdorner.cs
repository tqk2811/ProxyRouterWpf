using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ProxyRouterWpf.Views
{
    /// <summary>
    /// Lightweight, self-contained drag-drop feedback drawn over a grid's adorner layer.
    /// Two modes: an insertion line between rows (reorder) and a filled rectangle over a
    /// target row / whole grid (assign / ungroup). Never hit-test visible so it does not
    /// interfere with drop hit-testing.
    /// </summary>
    public sealed class DropAdorner : Adorner
    {
        enum Mode { None, InsertionLine, Highlight }

        readonly Pen _linePen;
        readonly Pen _borderPen;
        readonly Brush _fill;

        Mode _mode = Mode.None;
        double _lineY;
        Rect _rect;

        public DropAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;

            var accent = ResolveAccent();
            _linePen = new Pen(new SolidColorBrush(accent), 2);
            _linePen.Freeze();
            _borderPen = new Pen(new SolidColorBrush(accent), 1.5);
            _borderPen.Freeze();
            _fill = new SolidColorBrush(Color.FromArgb(48, accent.R, accent.G, accent.B));
            _fill.Freeze();
        }

        static Color ResolveAccent()
        {
            if (Application.Current?.TryFindResource("Brush.Accent") is SolidColorBrush b)
                return b.Color;
            return Color.FromRgb(0x00, 0x78, 0xD7); // fallback: Windows accent blue
        }

        /// <summary>Show a horizontal insertion line at <paramref name="y"/> (grid coordinates).</summary>
        public void SetLine(double y)
        {
            _mode = Mode.InsertionLine;
            _lineY = y;
            InvalidateVisual();
        }

        /// <summary>Highlight a rectangle (a target row or the whole grid), in grid coordinates.</summary>
        public void SetHighlight(Rect rect)
        {
            _mode = Mode.Highlight;
            _rect = rect;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            switch (_mode)
            {
                case Mode.InsertionLine:
                    double width = ((FrameworkElement)AdornedElement).ActualWidth;
                    drawingContext.DrawLine(_linePen, new Point(0, _lineY), new Point(width, _lineY));
                    drawingContext.DrawEllipse(_linePen.Brush, null, new Point(0, _lineY), 3, 3);
                    drawingContext.DrawEllipse(_linePen.Brush, null, new Point(width, _lineY), 3, 3);
                    break;
                case Mode.Highlight:
                    drawingContext.DrawRectangle(_fill, _borderPen, _rect);
                    break;
            }
        }
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProxyRouterWpf.Views.Controls
{
    /// <summary>
    /// A single input that holds both the date and the time of day. The text box is the source of
    /// truth (free typing, format <see cref="DisplayFormat"/>); the drop-down calendar + hour/minute
    /// combos are just a picker that writes back into it.
    /// </summary>
    public partial class DateTimePicker : UserControl
    {
        public const string DisplayFormat = "yyyy-MM-dd HH:mm";

        /// <summary>Formats accepted while typing, tried before the culture-aware fallback.</summary>
        static readonly string[] ParseFormats =
        {
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd",
            "yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd HH:mm", "yyyy/MM/dd",
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy",
        };

        /// <summary>Guards the text ⇄ value round trip so an edit does not immediately rewrite itself.</summary>
        bool _syncing;

        public DateTimePicker()
        {
            InitializeComponent();
            for (int h = 0; h < 24; h++) PART_Hour.Items.Add(h.ToString("00"));
            for (int m = 0; m < 60; m++) PART_Minute.Items.Add(m.ToString("00"));
        }

        public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register(
            nameof(SelectedDateTime), typeof(DateTime?), typeof(DateTimePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateTimeChanged));

        public DateTime? SelectedDateTime
        {
            get => (DateTime?)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder), typeof(string), typeof(DateTimePicker), new PropertyMetadata(DisplayFormat));

        /// <summary>Hint shown while the input is empty (rendered by the global TextBox style via Tag).</summary>
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((DateTimePicker)d).SyncFromValue();

        /// <summary>Pushes the current value into the text box and the picker widgets.</summary>
        void SyncFromValue()
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                var v = SelectedDateTime;
                PART_TextBox.Text = v?.ToString(DisplayFormat, CultureInfo.InvariantCulture) ?? string.Empty;
                PART_TextBox.CaretIndex = PART_TextBox.Text.Length;
                SyncPickers(v);
                MarkInvalid(false);
            }
            finally { _syncing = false; }
        }

        void SyncPickers(DateTime? v)
        {
            PART_Calendar.SelectedDate = v?.Date;
            if (v is not null) PART_Calendar.DisplayDate = v.Value.Date;
            PART_Hour.SelectedIndex = v?.Hour ?? -1;
            PART_Minute.SelectedIndex = v?.Minute ?? -1;
        }

        void PART_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                var text = PART_TextBox.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    SelectedDateTime = null;
                    SyncPickers(null);
                    MarkInvalid(false);
                    return;
                }

                if (TryParse(text, out var dt))
                {
                    SelectedDateTime = dt;
                    SyncPickers(dt);
                    MarkInvalid(false);
                }
                else
                {
                    // Half-typed text is not an error yet, but it must not keep filtering by a stale value.
                    SelectedDateTime = null;
                    MarkInvalid(true);
                }
            }
            finally { _syncing = false; }
        }

        static bool TryParse(string text, out DateTime value)
        {
            text = text.Trim();
            if (DateTime.TryParseExact(text, ParseFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                return true;
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value))
                return true;
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
        }

        void MarkInvalid(bool invalid)
            => Bd.BorderBrush = invalid
                ? (Brush)FindResource("Brush.Danger")
                : (Brush)FindResource("Brush.Control.BorderBrush");

        void PART_Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing || PART_Calendar.SelectedDate is not DateTime date) return;
            SelectedDateTime = date.Date + (SelectedDateTime?.TimeOfDay ?? TimeSpan.Zero);
        }

        void PART_Time_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing) return;
            int hour = Math.Max(0, PART_Hour.SelectedIndex);
            int minute = Math.Max(0, PART_Minute.SelectedIndex);
            // Picking a time before a date implies today.
            var date = SelectedDateTime?.Date ?? PART_Calendar.SelectedDate?.Date ?? DateTime.Today;
            SelectedDateTime = date + new TimeSpan(hour, minute, 0);
        }

        void Clear_Click(object sender, RoutedEventArgs e)
        {
            SelectedDateTime = null;
            SyncFromValue();
            PART_Popup.IsOpen = false;
        }
    }
}

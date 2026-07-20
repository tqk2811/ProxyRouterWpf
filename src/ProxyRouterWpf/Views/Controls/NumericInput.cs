using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProxyRouterWpf.Views.Controls
{
    /// <summary>
    /// Attached behaviour restricting a <see cref="TextBox"/> to digits, so a binding to an int
    /// property can never be fed something unparsable. Set <c>ctl:NumericInput.DigitsOnly="True"</c>.
    /// </summary>
    public static class NumericInput
    {
        public static readonly DependencyProperty DigitsOnlyProperty = DependencyProperty.RegisterAttached(
            "DigitsOnly", typeof(bool), typeof(NumericInput), new PropertyMetadata(false, OnDigitsOnlyChanged));

        public static void SetDigitsOnly(DependencyObject element, bool value) => element.SetValue(DigitsOnlyProperty, value);
        public static bool GetDigitsOnly(DependencyObject element) => (bool)element.GetValue(DigitsOnlyProperty);

        static void OnDigitsOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;
            if ((bool)e.NewValue)
            {
                tb.PreviewTextInput += OnPreviewTextInput;
                tb.PreviewKeyDown += OnPreviewKeyDown;
                DataObject.AddPastingHandler(tb, OnPaste);
            }
            else
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                tb.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }
        }

        static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !WouldStayNumeric((TextBox)sender, e.Text);

        /// <summary>Space does not raise PreviewTextInput in a TextBox, so it needs blocking here.</summary>
        static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) e.Handled = true;
        }

        static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var pasted = e.DataObject.GetDataPresent(DataFormats.UnicodeText)
                ? (string)e.DataObject.GetData(DataFormats.UnicodeText)
                : null;
            if (pasted is null || !WouldStayNumeric((TextBox)sender, pasted))
                e.CancelCommand();
        }

        /// <summary>Applies the pending edit to a copy of the text and checks the result is all digits.</summary>
        static bool WouldStayNumeric(TextBox tb, string insert)
        {
            var text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, insert);
            return text.All(char.IsAsciiDigit);
        }
    }
}

using System.Globalization;

namespace ProxyRouterWpf.Helpers
{
    /// <summary>One entry of the unit combobox next to a TotalBytes threshold input.</summary>
    public sealed class ByteUnit
    {
        public ByteUnit(string name, long multiplier)
        {
            Name = name;
            Multiplier = multiplier;
        }

        public string Name { get; }
        public long Multiplier { get; }
        public override string ToString() => Name;
    }

    /// <summary>
    /// Converts between the raw byte count stored in a TotalBytes filter and the
    /// value + unit pair the dialogs show, mirroring the web UI.
    /// </summary>
    public static class ByteThreshold
    {
        public static IReadOnlyList<ByteUnit> Units { get; } = new[]
        {
            new ByteUnit("B", 1L),
            new ByteUnit("KB", 1024L),
            new ByteUnit("MB", 1024L * 1024),
            new ByteUnit("GB", 1024L * 1024 * 1024),
            new ByteUnit("TB", 1024L * 1024 * 1024 * 1024),
        };

        /// <summary>
        /// Splits a raw byte count into the largest unit keeping the value &gt;= 1. Dividing by a
        /// power of two is exact in binary floating point, so multiplying back is lossless.
        /// </summary>
        public static (string Text, ByteUnit Unit) Split(long bytes)
        {
            if (bytes <= 0) return (bytes == 0 ? "0" : string.Empty, Units[0]);
            var unit = Units[0];
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                if (bytes >= Units[i].Multiplier) { unit = Units[i]; break; }
            }
            double value = bytes / (double)unit.Multiplier;
            return (value.ToString(CultureInfo.InvariantCulture), unit);
        }

        /// <summary>value × unit → byte count; false when the text is not a non-negative number.</summary>
        public static bool TryCompose(string? text, ByteUnit? unit, out long bytes)
        {
            bytes = 0;
            if (unit is null || string.IsNullOrWhiteSpace(text)) return false;
            // The input accepts both separators, so normalise before parsing invariantly.
            var normalized = text.Trim().Replace(',', '.');
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) return false;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) return false;
            double total = Math.Round(value * unit.Multiplier, MidpointRounding.AwayFromZero);
            if (total > long.MaxValue) return false;
            bytes = (long)total;
            return true;
        }
    }
}

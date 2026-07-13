using System.Globalization;

namespace ProxyRouterWpf.Helpers
{
    public static class BytesFormatter
    {
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            double v = bytes;
            string[] units = { "K", "M", "G", "T", "P", "E" };
            int i = -1;
            do
            {
                v /= 1024d;
                i++;
            } while (v >= 1024 && i < units.Length - 1);
            return $"{v.ToString("0.00", CultureInfo.InvariantCulture)} {units[i]}";
        }

        /// <summary>Formats a rate in bytes/second, e.g. "1.50 MB/s".</summary>
        public static string FormatRate(long bytesPerSec)
        {
            if (bytesPerSec < 1024) return $"{bytesPerSec} B/s";
            double v = bytesPerSec;
            string[] units = { "KB/s", "MB/s", "GB/s", "TB/s" };
            int i = -1;
            do
            {
                v /= 1024d;
                i++;
            } while (v >= 1024 && i < units.Length - 1);
            return $"{v.ToString("0.00", CultureInfo.InvariantCulture)} {units[i]}";
        }
    }
}

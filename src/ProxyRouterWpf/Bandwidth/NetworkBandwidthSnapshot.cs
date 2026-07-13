namespace ProxyRouterWpf.Bandwidth
{
    /// <summary>One whole-machine bandwidth sample (bytes/second). Immutable.</summary>
    public sealed class NetworkBandwidthSnapshot
    {
        public long BytesReceivedPerSec { get; }
        public long BytesSentPerSec { get; }
        public DateTime SampledAtUtc { get; }

        public NetworkBandwidthSnapshot(long bytesReceivedPerSec, long bytesSentPerSec, DateTime sampledAtUtc)
        {
            BytesReceivedPerSec = bytesReceivedPerSec;
            BytesSentPerSec = bytesSentPerSec;
            SampledAtUtc = sampledAtUtc;
        }
    }
}

using System.IO;
using ProxyRouterWpf.Proxy.EventLogs;

namespace ProxyRouterWpf.Proxy
{
    public partial class ProxySession
    {
        partial class MyPreProxyServerHandler
        {
            // Wraps the client socket stream: every Read counts as upload (client→upstream),
            // every Write counts as download (upstream→client). On Dispose (tunnel closed) it
            // commits the log with the accumulated byte totals. Does NOT close the inner stream —
            // the library already `using`s the base stream.
            sealed class BytesCountingStream : Stream
            {
                readonly Stream _inner;
                readonly ProxyTunnelLogState _state;
                readonly IProxyEventLogService _logService;
                readonly Guid _tunnelId;
                int _disposed;

                public BytesCountingStream(Stream inner, ProxyTunnelLogState state, IProxyEventLogService logService, Guid tunnelId)
                {
                    _inner = inner;
                    _state = state;
                    _logService = logService;
                    _tunnelId = tunnelId;
                }

                public override bool CanRead => _inner.CanRead;
                public override bool CanWrite => _inner.CanWrite;
                public override bool CanSeek => _inner.CanSeek;
                public override bool CanTimeout => _inner.CanTimeout;
                public override long Length => _inner.Length;
                public override long Position { get => _inner.Position; set => _inner.Position = value; }
                public override int ReadTimeout { get => _inner.ReadTimeout; set => _inner.ReadTimeout = value; }
                public override int WriteTimeout { get => _inner.WriteTimeout; set => _inner.WriteTimeout = value; }

                public override void Flush() => _inner.Flush();
                public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
                public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
                public override void SetLength(long value) => _inner.SetLength(value);

                public override int Read(byte[] buffer, int offset, int count)
                {
                    int n = _inner.Read(buffer, offset, count);
                    if (n > 0) _state.AddBytesUpload(n);
                    return n;
                }
                public override int Read(Span<byte> buffer)
                {
                    int n = _inner.Read(buffer);
                    if (n > 0) _state.AddBytesUpload(n);
                    return n;
                }
                public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    int n = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                    if (n > 0) _state.AddBytesUpload(n);
                    return n;
                }
                public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                {
                    int n = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (n > 0) _state.AddBytesUpload(n);
                    return n;
                }
                public override int ReadByte()
                {
                    int b = _inner.ReadByte();
                    if (b >= 0) _state.AddBytesUpload(1);
                    return b;
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    _inner.Write(buffer, offset, count);
                    if (count > 0) _state.AddBytesDownload(count);
                }
                public override void Write(ReadOnlySpan<byte> buffer)
                {
                    _inner.Write(buffer);
                    if (buffer.Length > 0) _state.AddBytesDownload(buffer.Length);
                }
                public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                    if (count > 0) _state.AddBytesDownload(count);
                }
                public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
                {
                    await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (buffer.Length > 0) _state.AddBytesDownload(buffer.Length);
                }
                public override void WriteByte(byte value)
                {
                    _inner.WriteByte(value);
                    _state.AddBytesDownload(1);
                }

                protected override void Dispose(bool disposing)
                {
                    if (Interlocked.Exchange(ref _disposed, 1) == 0)
                        _logService.Commit(_tunnelId, _state.PendingOutcome, _state.PendingRejectReason, _state.PendingErrorMessage);
                    base.Dispose(disposing);
                }
                public override ValueTask DisposeAsync()
                {
                    if (Interlocked.Exchange(ref _disposed, 1) == 0)
                        _logService.Commit(_tunnelId, _state.PendingOutcome, _state.PendingRejectReason, _state.PendingErrorMessage);
                    return base.DisposeAsync();
                }
            }
        }
    }
}

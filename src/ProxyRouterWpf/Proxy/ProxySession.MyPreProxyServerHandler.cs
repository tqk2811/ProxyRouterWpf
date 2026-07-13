using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using ProxyRouterWpf.Enums;
using TqkLibrary.Proxy.Handlers;
using TqkLibrary.Proxy.Interfaces;
using TqkLibrary.Proxy.ProxyServers;
using TqkLibrary.Proxy.StreamHelpers;

namespace ProxyRouterWpf.Proxy
{
    public partial class ProxySession
    {
        partial class MyPreProxyServerHandler : BasePreProxyServerHandler
        {
            readonly ProxySession _proxySession;
            readonly ILogger _logger;

            public MyPreProxyServerHandler(ProxySession proxySession)
                : base(proxySession._loggerFactory)
            {
                _proxySession = proxySession;
                _logger = proxySession._loggerFactory.CreateLogger<MyPreProxyServerHandler>();
            }

            public override Task<bool> IsAcceptClientAsync(
                TcpClient tcpClient,
                Guid tunnelId,
                CancellationToken cancellationToken = default)
            {
                var remoteEp = (IPEndPoint?)tcpClient.Client.RemoteEndPoint;
                var localEp = (IPEndPoint?)tcpClient.Client.LocalEndPoint;
                if (remoteEp != null)
                    _proxySession._logService.TrackTunnelStart(tunnelId, remoteEp, localEp);

                // IP whitelist removed in the WPF port — accept every client.
                return Task.FromResult(true);
            }

            // Wrap the socket stream at the pre-handler level — the wrapper lives exactly as long as
            // the TCP tunnel (ProxyServer._PreProxyWorkAsync `using`s it). When the tunnel closes,
            // Dispose commits the log with total upload/download. If the state was already
            // Committed/Discarded, return the raw stream.
            public override Task<Stream> StreamHandlerAsync(
                Stream stream,
                IPEndPoint iPEndPoint,
                Guid tunnelId,
                CancellationToken cancellationToken = default)
            {
                var logService = _proxySession._logService;
                var state = logService.GetState(tunnelId);
                if (state == null)
                    return Task.FromResult(stream);

                var wrapped = new BytesCountingStream(stream, state, logService, tunnelId);
                return Task.FromResult<Stream>(wrapped);
            }

            public override async Task<IProxyServer> GetProxyServerAsync(
                PreReadStream preReadStream,
                IPEndPoint iPEndPoint,
                Guid tunnelId,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    return await _GetProxyServerCoreAsync(preReadStream, iPEndPoint, tunnelId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var state = _proxySession._logService.GetState(tunnelId);
                    if (state != null)
                    {
                        _logger.LogWarning(ex, "GetProxyServer failed (will commit as PreReadFailed) TunnelId={TunnelId}, Remote={Remote}",
                            tunnelId, iPEndPoint);
                        state.PendingOutcome = ProxyTunnelOutcome.RequestFailed;
                        state.PendingRejectReason = ProxyTunnelRejectReason.PreReadFailed;
                        state.PendingErrorMessage = ex.Message;
                    }
                    throw;
                }
            }

            async Task<IProxyServer> _GetProxyServerCoreAsync(
                PreReadStream preReadStream,
                IPEndPoint iPEndPoint,
                Guid tunnelId,
                CancellationToken cancellationToken)
            {
                byte[] buffer = await preReadStream.PreReadAsync(1, cancellationToken).ConfigureAwait(false);
                if (buffer.Length == 0)
                {
                    _logger.LogWarning("GetProxyServer empty request TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                    _proxySession._logService.Commit(
                        tunnelId,
                        ProxyTunnelOutcome.RequestFailed,
                        ProxyTunnelRejectReason.EmptyRequest,
                        "Empty request");
                    throw new InvalidOperationException("Invalid Request");
                }

                var configure = _proxySession._configureService.Get();
                var logState = _proxySession._logService.GetState(tunnelId);

                switch (buffer[0])
                {
                    case 0x04:
                        if (logState != null) logState.ClientProtocol = ProxyTunnelClientProtocol.Socks4;
                        if (!configure.IsSocks4Enabled)
                        {
                            _logger.LogWarning("GetProxyServer reject SOCKS4 (disabled) TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                            _proxySession._logService.Commit(
                                tunnelId,
                                ProxyTunnelOutcome.RequestFailed,
                                ProxyTunnelRejectReason.Socks4Disabled,
                                "SOCKS4 is not enabled.");
                            throw new InvalidOperationException("SOCKS4 is not enabled.");
                        }
                        _logger.LogInformation("GetProxyServer protocol=SOCKS4 TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                        return new Socks4ProxyServer(_loggerFactory);

                    case 0x05:
                        if (logState != null) logState.ClientProtocol = ProxyTunnelClientProtocol.Socks5;
                        if (!configure.IsSocks5Enabled)
                        {
                            _logger.LogWarning("GetProxyServer reject SOCKS5 (disabled) TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                            _proxySession._logService.Commit(
                                tunnelId,
                                ProxyTunnelOutcome.RequestFailed,
                                ProxyTunnelRejectReason.Socks5Disabled,
                                "SOCKS5 is not enabled.");
                            throw new InvalidOperationException("SOCKS5 is not enabled.");
                        }
                        _logger.LogInformation("GetProxyServer protocol=SOCKS5 TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                        return new Socks5ProxyServer(_loggerFactory);

                    default:
                        string header = await preReadStream.PreReadLineAsync(32 * 1024, cancellationToken).ConfigureAwait(false);
                        if (header.Contains("HTTP/", StringComparison.OrdinalIgnoreCase))
                        {
                            if (logState != null) logState.ClientProtocol = ProxyTunnelClientProtocol.Http;
                            if (!configure.IsHttpEnabled)
                            {
                                _logger.LogWarning("GetProxyServer reject HTTP (disabled) TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                                _proxySession._logService.Commit(
                                    tunnelId,
                                    ProxyTunnelOutcome.RequestFailed,
                                    ProxyTunnelRejectReason.HttpDisabled,
                                    "HTTP is not enabled.");
                                throw new InvalidOperationException("HTTP is not enabled.");
                            }
                            _logger.LogInformation("GetProxyServer protocol=HTTP TunnelId={TunnelId}, Remote={Remote}", tunnelId, iPEndPoint);
                            return new HttpProxyServer(_loggerFactory);
                        }
                        _logger.LogWarning("GetProxyServer unknown protocol TunnelId={TunnelId}, Remote={Remote}, FirstByte=0x{FirstByte:X2}",
                            tunnelId, iPEndPoint, buffer[0]);
                        _proxySession._logService.Commit(
                            tunnelId,
                            ProxyTunnelOutcome.RequestFailed,
                            ProxyTunnelRejectReason.UnknownProtocol,
                            $"Unknown protocol, first byte 0x{buffer[0]:X2}");
                        throw new InvalidOperationException("Invalid Request");
                }
            }

            public override Task OnExceptionAsync(IPEndPoint iPEndPoint, Guid tunnelId, Exception exception)
            {
                var state = _proxySession._logService.GetState(tunnelId);
                if (state == null)
                    return Task.CompletedTask;

                if (state.PendingOutcome == ProxyTunnelOutcome.Resolved)
                {
                    state.PendingOutcome = ProxyTunnelOutcome.RouteFailed;
                    state.PendingRejectReason = ProxyTunnelRejectReason.TunnelAborted;
                    state.PendingErrorMessage = exception.Message;
                }
                return Task.CompletedTask;
            }
        }
    }
}

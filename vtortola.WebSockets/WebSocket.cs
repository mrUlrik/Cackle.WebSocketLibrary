using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace vtortola.WebSockets
{
    [PublicAPI]
    public abstract class WebSocket : IDisposable
    {

        /// <summary>
        /// Request message of WebSocket negotiation.
        /// </summary>
        [NotNull]
        public WebSocketHttpRequest HttpRequest { get; }
        /// <summary>
        /// Response message of WebSocket negotiation.
        /// </summary>
        [NotNull]
        public WebSocketHttpResponse HttpResponse { get; }

        /// <summary>
        /// True if it is possible to send OR receive message via current <see cref="WebSocket"/>.
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Address of connection's remote endpoint.
        /// </summary>
        public abstract EndPoint RemoteEndpoint { get; }
        /// <summary>
        /// Address of connection's local endpoint.
        /// </summary>
        public abstract EndPoint LocalEndpoint { get; }
        /// <summary>
        /// Last ping latency. Available when <see cref="WebSocketListenerOptions.PingMode"/> is set to <see cref="PingMode.LatencyControl"/>.
        /// </summary>
        public abstract TimeSpan Latency { get; }
        /// <summary>
        /// Sub-protocol negotiated with remote party.
        /// </summary>
        public abstract string SubProtocol { get; }
        /// <summary>
        /// <see cref="WebSocket"/> connection close reason sent by remote party.
        /// </summary>
        public abstract WebSocketCloseReason? CloseReason { get; }

        /// <summary>
        /// Constructor of <see cref="WebSocket"/>.
        /// </summary>
        protected WebSocket([NotNull] WebSocketHttpRequest request, [NotNull] WebSocketHttpResponse response)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (response == null) throw new ArgumentNullException(nameof(response));

            this.HttpRequest = request;
            this.HttpResponse = response;
        }

        /// <summary>
        /// Start reading incoming message.
        /// </summary>
        /// <param name="token">Reading operation cancellation. If operation is cancelled, <see cref="WebSocket"/> considered broken and should be closed with <see cref="CloseAsync()"/>.</param>
        /// <returns>Readable payload stream-or-null if other side is sent close message.
        /// If null message is received called should send all remaining data with <see cref="CreateMessageWriter"/> and finish <see cref="WebSocket"/> with <see cref="CloseAsync()"/>.</returns>
        [NotNull, ItemCanBeNull]
        public abstract Task<WebSocketMessageReadStream> ReadMessageAsync(CancellationToken token);

        /// <summary>
        /// Start writing outgoing message of specified <paramref name="messageType"/>.
        /// </summary>
        /// <param name="messageType">Type of message. Binary or text (utf8 encoded).</param>
        /// <returns>Write-able message payload stream.</returns>
        [NotNull]
        public abstract WebSocketMessageWriteStream CreateMessageWriter(WebSocketMessageType messageType);

        /// <summary>
        /// Manually sent ping frame. Used when <see cref="WebSocketListenerOptions.PingMode"/> is set to <see cref="PingMode.Manual"/>.
        /// </summary>
        public Task SendPingAsync()
        {
            return this.SendPingAsync(null, 0, 0);
        }

        /// <summary>
        /// Manually sent ping frame. Used when <see cref="WebSocketListenerOptions.PingMode"/> is set to <see cref="PingMode.Manual"/>.
        /// </summary>
        /// <param name="data">Ping payload buffer.</param>
        /// <param name="offset">Ping payload offer.</param>
        /// <param name="count">Ping payload size.</param>
        /// <returns></returns>
        public abstract Task SendPingAsync(byte[] data, int offset, int count);

        /// <summary>
        /// Closes underlying connection with specified <see cref="WebSocketCloseReason.NormalClose"/>. Will not throw exceptions except <see cref="ObjectDisposedException"/> if called after <see cref="Dispose()"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If called after <see cref="WebSocket"/> being disposed.</exception>
        public abstract Task CloseAsync();

        /// <summary>
        /// Closes underlying connection with specified <paramref name="closeCode"/>. Will not throw exceptions except <see cref="ObjectDisposedException"/> if called after <see cref="Dispose()"/>.
        /// </summary>
        /// <param name="closeCode"></param>
        /// <returns>Close task.</returns>
        /// <exception cref="ObjectDisposedException">If called after <see cref="WebSocket"/> being disposed.</exception>
        public abstract Task CloseAsync(WebSocketCloseReason closeCode);

        /// <summary>
        /// Abort current <see cref="WebSocket"/> negotiation, close connection and release underlying resources. To perform graceful closure call <see cref="CloseAsync()"/> before dispose.
        /// </summary>
        public abstract void Dispose();

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.GetType().Name}, remote: {this.RemoteEndpoint}, connected: {this.IsConnected}";
        }
    }
}

using System.Runtime.ExceptionServices;
using vtortola.WebSockets.Async;
using vtortola.WebSockets.Tools;
using vtortola.WebSockets.Transports;

namespace vtortola.WebSockets.Http
{
    internal sealed class HttpNegotiationQueue : IDisposable
    {
        private readonly ILogger log;
        private readonly AsyncQueue<NetworkConnection> _connections;
        private readonly AsyncQueue<WebSocketNegotiationResult> _negotiations;
        private readonly CancellationTokenSource _cancel;
        private readonly WebSocketHandshaker _handShaker;
        private readonly WebSocketListenerOptions _options;
        private readonly WebSocketConnectionExtensionCollection _extensions;
        private readonly SemaphoreSlim _semaphore;
        private readonly PingQueue pingQueue;

        public HttpNegotiationQueue(WebSocketFactoryCollection standards, WebSocketConnectionExtensionCollection extensions, WebSocketListenerOptions options)
        {
            if (standards == null) throw new ArgumentNullException(nameof(standards));
            if (extensions == null) throw new ArgumentNullException(nameof(extensions));
            if (options == null) throw new ArgumentNullException(nameof(options));

            this.log = options.Logger;

            _options = options;
            _extensions = extensions;
            _cancel = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(options.ParallelNegotiations);

            _connections = new AsyncQueue<NetworkConnection>(options.NegotiationQueueCapacity);
            _negotiations = new AsyncQueue<WebSocketNegotiationResult>();
            _negotiations.ParallelTakeErrorMessage = $"Parallel call to '{nameof(WebSocketListener.AcceptWebSocketAsync)}' is not allowed.";
            _negotiations.ClosedErrorMessage = $"{nameof(WebSocketListener)} is closed and will not accept new connections.";

            //_cancel.Token.Register(() => this._connections.Close(new OperationCanceledException()));

            _handShaker = new WebSocketHandshaker(standards, _options);

            if (options.PingMode != PingMode.Manual)
                this.pingQueue = new PingQueue(options.PingInterval);

            WorkAsync().LogFault(this.log);
        }

        private async Task WorkAsync()
        {
            await Task.Yield();
            while (!_cancel.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(_cancel.Token).ConfigureAwait(false);
                    var socket = await this._connections.DequeueAsync(_cancel.Token).ConfigureAwait(false);
                    NegotiateWebSocket(socket).LogFault(this.log);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception negotiateError)
                {
                    if (this.log.IsWarningEnabled && negotiateError.Unwrap() is OperationCanceledException == false)
                        this.log.Warning("An error occurred while negotiating WebSocket request.", negotiateError.Unwrap());
                    _cancel.Cancel();
                }
            }
        }

        private async Task NegotiateWebSocket(NetworkConnection networkConnection)
        {
            if (networkConnection == null) throw new ArgumentNullException(nameof(networkConnection));

            await Task.Yield();

            WebSocketNegotiationResult result;
            try
            {
                var negotiationTimeout = this._options.NegotiationTimeout;

                var timeoutTask = Task.Delay(negotiationTimeout);
                var startTime = DateTime.UtcNow;

                foreach (var connectionExtension in _extensions)
                {
                    var extTask = connectionExtension.ExtendConnectionAsync(networkConnection);
                    await Task.WhenAny(timeoutTask, extTask).ConfigureAwait(false);
                    if (timeoutTask.IsCompleted)
                    {
#pragma warning disable 4014
                        extTask.IgnoreFaultOrCancellation(); // make connection exception observed
#pragma warning restore 4014
                        throw new WebSocketException($"Negotiation timeout (Extension: {connectionExtension.GetType().Name})");
                    }

                    networkConnection = await extTask.ConfigureAwait(false);
                }

                var handshakeTask = _handShaker.HandshakeAsync(networkConnection);
                await Task.WhenAny(timeoutTask, handshakeTask).ConfigureAwait(false);
                if (timeoutTask.IsCompleted)
                {
#pragma warning disable 4014
                    handshakeTask.IgnoreFaultOrCancellation(); // make connection exception observed
#pragma warning restore 4014
                    var message = $"Negotiation timeout: {(DateTime.UtcNow - startTime).TotalMilliseconds:F0} ms left. Expected: not greater than {negotiationTimeout.Milliseconds} ms";

                    throw new WebSocketException(message);
                }

                var handshake = await handshakeTask.ConfigureAwait(false);

                if (handshake.IsValidWebSocketRequest)
                {
                    result = new WebSocketNegotiationResult(handshake.Factory.CreateWebSocket(networkConnection, _options, handshake.Request, handshake.Response, handshake.NegotiatedMessageExtensions));
                }
                else if (handshake.IsValidHttpRequest && _options.HttpFallback != null)
                {
                    _options.HttpFallback.Post(handshake.Request, networkConnection);
                    return;
                }
                else
                {
                    SafeEnd.Dispose(networkConnection, this.log);
                    result = new WebSocketNegotiationResult(handshake.Error);
                }

                var webSocket = result.Result;
                if (_negotiations.TryEnqueue(result) == false)
                {
                    SafeEnd.Dispose(webSocket);
                    return; // too many negotiations
                }

                if (webSocket != null)
                    this.pingQueue?.GetSubscriptionList().Add(webSocket);
            }
            catch (Exception negotiationError)
            {
                if (this.log.IsDebugEnabled)
                    this.log.Debug("An error occurred while negotiating WebSocket request.", negotiationError);

                SafeEnd.Dispose(networkConnection, this.log);
                result = new WebSocketNegotiationResult(ExceptionDispatchInfo.Capture(negotiationError));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Queue(NetworkConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (!this._connections.TryEnqueue(connection))
            {
                if (this.log.IsWarningEnabled)
                    this.log.Warning($"Negotiation queue is full and can't process new connection from '{connection.RemoteEndPoint}'. Connection will be closed.");
                connection.CloseAsync().ContinueWith(_ => SafeEnd.Dispose(connection, this.log), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        public AsyncQueue<WebSocketNegotiationResult>.TakeResult DequeueAsync(CancellationToken cancel)
        {
            return _negotiations.DequeueAsync(cancel);
        }

        public void Dispose()
        {
            SafeEnd.Dispose(_semaphore, this.log);

            _cancel?.Cancel(throwOnFirstException: false);
            foreach (var connection in this._connections.TakeAllAndClose(closeError: new OperationCanceledException()))
                SafeEnd.Dispose(connection, this.log);
            foreach (var negotiation in this._negotiations.TakeAllAndClose())
                SafeEnd.Dispose(negotiation.Result, this.log);

            SafeEnd.Dispose(this.pingQueue, this.log);
        }
    }
}

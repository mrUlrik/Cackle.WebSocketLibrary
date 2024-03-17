using vtortola.WebSockets.Deflate;
using vtortola.WebSockets.Rfc6455;
using vtortola.WebSockets.Transports.NamedPipes;

namespace vtortola.WebSockets.UnitTests
{
    public class WebSocketClientTest
    {
        [Test]
        public void ConstructTest()
        {
            var options = new WebSocketListenerOptions() { Logger = new TestLogger() };
            options.Standards.RegisterRfc6455();
            var webSocketClient = new WebSocketClient(options);

            Assert.NotNull(webSocketClient);
        }

        [Theory]
        [TestCase("ws://echo.websocket.org?encoding=text", 15)]
        [TestCase("wss://echo.websocket.org?encoding=text", 15)]
        public async Task RemoteConnectToServerAsync(string address, int timeoutSeconds)
        {
            var options = new WebSocketListenerOptions()
            {
                NegotiationTimeout = TimeSpan.FromSeconds(20),
                Logger = new TestLogger() { IsDebugEnabled = System.Diagnostics.Debugger.IsAttached },
            };
            options.Standards.RegisterRfc6455();
            var webSocketClient = new WebSocketClient(options);

            var timeout = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var connectTask = webSocketClient.ConnectAsync(new Uri(address), CancellationToken.None);

            if (await Task.WhenAny(connectTask, timeout).ConfigureAwait(false) == timeout)
                throw new TimeoutException();

            var webSocket = await connectTask.ConfigureAwait(false);
            await webSocket.CloseAsync().ConfigureAwait(false);
        }

        [Theory]
        [TestCase("ws://echo.websocket.org/?encoding=text", 15, new[] { "a test message" })]
        [TestCase("ws://echo.websocket.org/?encoding=text", 15, new[] { "a test message", "a second message" })]
        [TestCase("wss://echo.websocket.org?encoding=text", 15, new[] { "a test message" })]
        [TestCase("wss://echo.websocket.org?encoding=text", 15, new[] { "a test message", "a second message" })]
        public async Task RemoteEchoServerAsync(string address, int timeoutSeconds, string[] messages)
        {
            var timeout = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
            var options = new WebSocketListenerOptions
            {
                NegotiationTimeout = TimeSpan.FromSeconds(20),
                Logger = new TestLogger() { IsDebugEnabled = System.Diagnostics.Debugger.IsAttached }
            };
            options.Standards.RegisterRfc6455();
            var webSocketClient = new WebSocketClient(options);
            var connectTask = webSocketClient.ConnectAsync(new Uri(address), CancellationToken.None);

            if (await Task.WhenAny(connectTask, timeout).ConfigureAwait(false) == timeout)
                throw new TimeoutException();

            var webSocket = await connectTask.ConfigureAwait(false);

            var sendReceiveTask = new Func<Task>(async () =>
            {
                await Task.Yield();
                foreach (var message in messages)
                {
                    await webSocket.WriteStringAsync(message, cancellation).ConfigureAwait(false);
                    TestContext.Out.WriteLine("[CLIENT] -> " + message);
                    await Task.Delay(100).ConfigureAwait(false);
                }

                foreach (var expectedMessage in messages)
                {
                    var actualMessage = await webSocket.ReadStringAsync(cancellation).ConfigureAwait(false);
                    if (actualMessage == null && !webSocket.IsConnected) throw new InvalidOperationException("Connection is closed!");

                    TestContext.Out.WriteLine("[CLIENT] <- " + (actualMessage ?? "<null>"));
                    Assert.NotNull(actualMessage);
                    Assert.That(actualMessage, Is.EqualTo(expectedMessage));
                }

                await webSocket.CloseAsync().ConfigureAwait(false);
            })();

            if (await Task.WhenAny(sendReceiveTask, timeout).ConfigureAwait(false) == timeout)
                throw new TimeoutException();

            await sendReceiveTask.ConfigureAwait(false);
        }

        [Theory]
        [TestCase("tcp://localhost:10000/", 15, new[] { "a test message" })]
        [TestCase("tcp://localhost:10001/", 15, new[] { "a test message", "a second message" })]
        [TestCase("tcp://127.0.0.1:10002/", 15, new[] { "a test message" })]
        [TestCase("tcp://127.0.0.1:10003/", 15, new[] { "a test message", "a second message" })]
        [TestCase("pipe://testpipe/", 15, new[] { "a test message" })]
        [TestCase("pipe://testpipe1/", 15, new[] { "a test message", "a second message" })]
        public async Task EchoServerAsync(string address, int timeoutSeconds, string[] messages)
        {
            var maxClients = 1;
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
            var options = new WebSocketListenerOptions
            {
                Logger = new TestLogger() { IsDebugEnabled = System.Diagnostics.Debugger.IsAttached }
            };
            options.Standards.RegisterRfc6455(rfc6455 =>
            {
                rfc6455.MessageExtensions.Add(new WebSocketDeflateExtension());
            });
            options.Transports.RegisterNamedPipes();

            var listenEndPoints = new[] { new Uri(address) };
            var server = new EchoServer(listenEndPoints, options);

            TestContext.Out.WriteLine("[TEST] Starting echo server.");
            await server.StartAsync().ConfigureAwait(false);

            var messageSender = new MessageSender(listenEndPoints[0], options);
            TestContext.Out.WriteLine("[TEST] Connecting clients.");
            await messageSender.ConnectAsync(maxClients, cancellation).ConfigureAwait(false);
            TestContext.Out.WriteLine($"[TEST] {messageSender.ConnectedClients} Client connected.");
            TestContext.Out.WriteLine($"[TEST] Sending {maxClients * messages.Length} messages.");
            var sendTask = messageSender.SendMessagesAsync(messages, cancellation);
            while (sendTask.IsCompleted == false)
            {
                await Task.Delay(1000);
                TestContext.Out.WriteLine($"[TEST] Server: r={server.ReceivedMessages}, s={server.SentMessages}, e={server.Errors}. " +
                    $"Clients: r={messageSender.MessagesReceived}, s={messageSender.MessagesSent}, e={messageSender.Errors}.");
            }
            var processedMessages = await sendTask.ConfigureAwait(false);
            Assert.That(processedMessages, Is.EqualTo(messages.Length));

            TestContext.Out.WriteLine("[TEST] Stopping echo server.");
            await server.StopAsync().ConfigureAwait(false);
            TestContext.Out.WriteLine("[TEST] Echo server stopped.");
            TestContext.Out.WriteLine("[TEST] Disconnecting clients.");
            await messageSender.CloseAsync().ConfigureAwait(false);
            TestContext.Out.WriteLine("[TEST] Disposing server.");
            server.Dispose();

            TestContext.Out.WriteLine("[TEST] Waiting for send/receive completion.");
        }

        [Theory]
        [TestCase("tcp://127.0.0.1:10100/", 10, 10)]
        [TestCase("tcp://127.0.0.1:10101/", 20, 100)]
        //[TestCase("tcp://127.0.0.1:10102/", 30, 1000)]
        //[TestCase("tcp://127.0.0.1:10103/", 40, 10000)]
        public async Task EchoServerMassClientsAsync(string address, int timeoutSeconds, int maxClients)
        {
            var messages = new string[] { new string('a', 126), new string('a', 127), new string('a', 128), new string('a', ushort.MaxValue - 1), new string('a', ushort.MaxValue), new string('a', ushort.MaxValue + 2) };
            var startTime = DateTime.UtcNow;
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
            var options = new WebSocketListenerOptions
            {
                NegotiationQueueCapacity = maxClients,
                PingTimeout = TimeSpan.FromSeconds(30),
                Logger = new TestLogger() { IsDebugEnabled = System.Diagnostics.Debugger.IsAttached }
            };
            options.Standards.RegisterRfc6455();
            options.Transports.ConfigureTcp(tcp =>
            {
                tcp.NoDelay = false;
                tcp.BacklogSize = maxClients;
                tcp.SendTimeout = TimeSpan.FromSeconds(15);
                tcp.ReceiveTimeout = TimeSpan.FromSeconds(15);
            });
            options.Transports.Add(new NamedPipeTransport());
            var listenEndPoints = new[] { new Uri(address) };
            var server = new EchoServer(listenEndPoints, options);

            TestContext.Out.WriteLine("[TEST] Starting echo server.");
            await server.StartAsync().ConfigureAwait(false);

            var messageSender = new MessageSender(listenEndPoints[0], options);
            TestContext.Out.WriteLine("[TEST] Connecting clients.");
            await messageSender.ConnectAsync(maxClients, cancellation).ConfigureAwait(false);
            TestContext.Out.WriteLine($"[TEST] {messageSender.ConnectedClients} Client connected.");
            TestContext.Out.WriteLine($"[TEST] Sending {maxClients * messages.Length} messages.");
            var sendTask = messageSender.SendMessagesAsync(messages, cancellation);
            while (sendTask.IsCompleted == false && cancellation.IsCancellationRequested == false)
            {
                await Task.Delay(1000);
                TestContext.Out.WriteLine($"[TEST] T:{timeoutSeconds - (DateTime.UtcNow - startTime).TotalSeconds:F0} " +
                    $"Server: r={server.ReceivedMessages}, s={server.SentMessages}, e={server.Errors}. " +
                    $"Clients: r={messageSender.MessagesReceived}, s={messageSender.MessagesSent}, e={messageSender.Errors}.");
            }

            var errorMessages = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            server.PushErrorMessagesTo(errorMessages);
            messageSender.PushErrorMessagesTo(errorMessages);

            if (errorMessages.Count > 0)
            {
                TestContext.Out.WriteLine("Errors:");
                foreach (var kv in errorMessages)
                    TestContext.Out.WriteLine($"[TEST] [x{kv.Value}] {kv.Key}");
            }


            if (cancellation.IsCancellationRequested)
                throw new TimeoutException();

            var processedMessages = await sendTask.ConfigureAwait(false);
            Assert.That(processedMessages, Is.EqualTo(messages.Length * maxClients));

            TestContext.Out.WriteLine("[TEST] Stopping echo server.");
            await server.StopAsync().ConfigureAwait(false);
            TestContext.Out.WriteLine("[TEST] Echo server stopped.");
            TestContext.Out.WriteLine("[TEST] Disconnecting clients.");
            await messageSender.CloseAsync().ConfigureAwait(false);
            TestContext.Out.WriteLine("[TEST] Disposing server.");
            server.Dispose();

            TestContext.Out.WriteLine("[TEST] Waiting for send/receive completion.");
        }
    }
}

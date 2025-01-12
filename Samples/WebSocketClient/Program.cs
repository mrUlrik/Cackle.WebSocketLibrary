using EchoServer;
using Serilog;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

// configuring logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, __) => cancellationTokenSource.Cancel();

Console.WriteLine("Press CTRL+C to stop client.");
Console.WriteLine("Press ESC to gracefully close connection.");

var bufferSize = 1024 * 8; // 8KiB
var bufferPoolSize = 100 * bufferSize; // 800KiB pool

var options = new WebSocketListenerOptions
{
    // set send buffer size (optional but recommended)
    SendBufferSize = bufferSize,
    // set buffer manager for buffers re-use (optional but recommended)
    BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize),
    // set logger, leave default NullLogger if you don't want logging
    Logger = new SerilogLogger()
};
// register RFC6455 protocol implementation (required)
options.Standards.RegisterRfc6455();
// configure tcp transport (optional)
options.Transports.ConfigureTcp(tcp =>
{
    tcp.BacklogSize = 100; // max pending connections waiting to be accepted
    tcp.ReceiveBufferSize = bufferSize;
    tcp.SendBufferSize = bufferSize;
});

var message = "Hello!";
var echoServerUrl = new Uri("wss://echo.websocket.org?encoding=text");
var client = new vtortola.WebSockets.WebSocketClient(options);

Log.Warning("Connecting to " + echoServerUrl + "...");
var webSocket = client.ConnectAsync(echoServerUrl, cancellationTokenSource.Token).Result;
Log.Warning("Connected to " + echoServerUrl + ". ");

while (cancellationTokenSource.IsCancellationRequested == false)
{
    Log.Warning("Sending text: " + message);
    webSocket.WriteStringAsync(message, cancellationTokenSource.Token).Wait();

    var responseText = webSocket.ReadStringAsync(cancellationTokenSource.Token).Result;
    Log.Warning("Received message:" + responseText);

    if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
        break;
    Thread.Sleep(400);
}

Log.Warning("Disconnecting from " + echoServerUrl + "...");
webSocket.CloseAsync().Wait();
Log.Warning("Disconnected from " + echoServerUrl + ".");

Log.Warning("Disposing client...");
client.CloseAsync().Wait();

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

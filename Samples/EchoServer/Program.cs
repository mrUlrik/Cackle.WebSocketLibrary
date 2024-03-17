﻿using System.Diagnostics;
using EchoServer;
using Serilog;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;
using vtortola.WebSockets.Rfc6455;

// configuring logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Warning("Starting Echo Server");
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

var cancellation = new CancellationTokenSource();

var bufferSize = 1024 * 8; // 8KiB
var bufferPoolSize = 100 * bufferSize; // 800KiB pool

var options = new WebSocketListenerOptions
{
    SubProtocols = new[] { "text" },
    PingTimeout = TimeSpan.FromSeconds(5),
    NegotiationTimeout = TimeSpan.FromSeconds(5),
    PingMode = PingMode.Manual,
    ParallelNegotiations = 16,
    NegotiationQueueCapacity = 256,
    SendBufferSize = bufferSize,
    BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize),
    Logger = new SerilogLogger()
};
options.Standards.RegisterRfc6455(factory =>
{
    factory.MessageExtensions.RegisterDeflateCompression();
});
// configure tcp transport
options.Transports.ConfigureTcp(tcp =>
{
    tcp.BacklogSize = 100; // max pending connections waiting to be accepted
    tcp.ReceiveBufferSize = bufferSize;
    tcp.SendBufferSize = bufferSize;
});

// adding the WSS extension
//var certificate = new X509Certificate2(File.ReadAllBytes("<PATH-TO-CERTIFICATE>"), "<PASSWORD>");
// options.ConnectionExtensions.RegisterSecureConnection(certificate);

var listenEndPoints = new Uri[] {
                new Uri("ws://localhost") // will listen both IPv4 and IPv6
            };

// starting the server
var server = new WebSocketListener(listenEndPoints, options);

server.StartAsync().Wait();

Log.Warning("Echo Server listening: " + string.Join(", ", Array.ConvertAll(listenEndPoints, e => e.ToString())) + ".");
Log.Warning("You can test echo server at http://www.websocket.org/echo.html.");

var acceptingTask = AcceptWebSocketsAsync(server, cancellation.Token);

Log.Warning("Press any key to stop.");
Console.ReadKey(true);

Log.Warning("Server stopping.");
cancellation.Cancel();
server.StopAsync().Wait();
acceptingTask.Wait();
return;

static async Task AcceptWebSocketsAsync(WebSocketListener server, CancellationToken cancellation)
{
    await Task.Yield();

    while (!cancellation.IsCancellationRequested)
    {
        try
        {
            var webSocket = await server.AcceptWebSocketAsync(cancellation).ConfigureAwait(false);
            if (webSocket == null)
            {
                if (cancellation.IsCancellationRequested || !server.IsStarted)
                    break; // stopped

                continue; // retry
            }

#pragma warning disable 4014
            EchoAllIncomingMessagesAsync(webSocket, cancellation);
#pragma warning restore 4014
        }
        catch (OperationCanceledException)
        {
            /* server is stopped */
            break;
        }
        catch (Exception acceptError)
        {
            Log.Error("An error occurred while accepting client.", acceptError);
        }
    }

    Log.Warning("Server has stopped accepting new clients.");
}

static async Task EchoAllIncomingMessagesAsync(WebSocket webSocket, CancellationToken cancellation)
{
    Log.Warning("Client '" + webSocket.RemoteEndpoint + "' connected.");
    var sw = new Stopwatch();
    try
    {
        while (webSocket.IsConnected && !cancellation.IsCancellationRequested)
        {
            try
            {
                var messageText = await webSocket.ReadStringAsync(cancellation).ConfigureAwait(false);
                if (messageText == null)
                    break; // webSocket is disconnected

                sw.Restart();

                await webSocket.WriteStringAsync(messageText, cancellation).ConfigureAwait(false);

                Log.Warning("Client '" + webSocket.RemoteEndpoint + "' sent: " + messageText + ".");

                sw.Stop();
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception readWriteError)
            {
                Log.Error("An error occurred while reading/writing echo message.", readWriteError);
                break;
            }
        }

        // close socket before dispose
        await webSocket.CloseAsync(WebSocketCloseReason.NormalClose);
    }
    finally
    {
        // always dispose socket after use
        webSocket.Dispose();
        Log.Warning("Client '" + webSocket.RemoteEndpoint + "' disconnected.");
    }
}

static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
{
    Log.Error("Unobserved Exception: ", e.Exception);
}
static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    Log.Error("Unhandled Exception: ", e.ExceptionObject as Exception);
}
[![Build status](https://ci.appveyor.com/api/projects/status/o2rf2om0dus3fd4f?svg=true)](https://ci.appveyor.com/project/deniszykov/websocketlistener)

WebSocketListener 
=================

The **WebSocketListener** class provides simple methods that listen for and accept incoming WebSocket connection requests asynchronously. It is a lightweight listener with an API very similar to the `System.Net.TcpListener` class.

It **does not use** the Microsoft's `System.Net.WebSockets` namespace. It has been reworked to use *.NET 8.0* and should work on any operating system with .NET 8.0 support.

**WebSocketListener** has been designed to provide WebSocket connectivity to other applications, in the same way that `System.Net.TcpListener` provides TCP connectivity. It is not a communication framework on its own and it does not provide any kind of publisher/subscriber pattern or reliable messaging beyond TCP.

 * It can work with both **Text or Binary** messages.
 * It supports `wss://`(secure). [More info](//github.com/vtortola/WebSocketListener/wiki/Enabling-WebSocket-Secure-(TLS)).
 * It supports **per-message deflate compression**. [More info](//github.com/vtortola/WebSocketListener/wiki/Deflate-extension). 
 * It can work with multiple WebSocket standards simultaneously. [More info](//github.com/vtortola/WebSocketListener/wiki/Multiple-WebSocket-standard-support)
 * It is **extensible**. [More info](//github.com/vtortola/WebSocketListener/wiki/WebSocketListener-Extensions).
 * It is **asynchronous**. 
 * It supports **Mono**. [More info] (//github.com/vtortola/WebSocketListener/wiki/Mono-support)
 * It supports **.NET Framework 4.5 and up**, **NETStandard 1.3 and up** and ~~Universal Windows Platform 10 (uap10.0)~~.
 * It has the [**Ping/Pong** functionality](http://tools.ietf.org/html/rfc6455#section-5.5.2) **built-in**.
 * It can measure **connection latency**. [More info](//github.com/vtortola/WebSocketListener/wiki/Measuring-WebSockets-connection-latency)
 * It can work with ~~cookies~~ and custom HTTP response statuses. [More info](//github.com/vtortola/WebSocketListener/wiki/Hooking-into-the-HTTP-negotiation)
 * It detects and disconnects half open connections.
 * It allows to **send and receive messages as streams**. WebSocket messages are represented as delimited stream-like objects, that allows integration with other .NET objects like e.g. `StreamReader` and `StreamWriter`. Two different WebSocket messages, yield two different streams.
 * Messages reads and writes are streamed. Big messages are not held in memory during reads or writes.
 * It handles partial frames transparently. The WebSocket specification states that a single message can be sent across multiple individual frames. The message stream will allow to read all the message data, no matter if it was sent in a single or multiple frames.
 * It handles interleaved control frames transparently. The WebSocket specification states that control frames can appear interleaved with data frames, including between partial frames of the same message. The message stream will allow to read just the message data, it will skip the control frames.

Take a look on the [performance and load  tests](//github.com/vtortola/WebSocketListener/wiki/WebSocketListener-performance-tests) on a simple 'echo' server.

What's new in v4 (this fork)
=================
This is a fork from [project](https://github.com/vtortola/WebSocketListener). 
There is some new features and bug fixes.

Major Features:
- WebSocketClient
    - Same features and options as WebSocketListener
    - TLS support
    - works with IPv4/IPv6
    
- Transports
    - TCP, Unix Sockets, Named Pipes (why not)
    - System.Net.Socket is abstracted as NetworkConnection
    - Graceful disconnection
    - Fine tuning with WebSocketListenerOptions. Fluent confuration API.

Minor Features:
- Fully async API (it is strongly discouraged to use synchronous API with IO operations)
- Logging via abstract ILogger
- No additional dependencies (ServiceModel, TPL Dataflow)
- New Headers<HeadersT> collections with fast access to known headers
- Custom BufferManager, and use of BufferManager in all operations with buffers
- WebSocketListener can now listen multiple endpoints from different transports
- Pings now processed in batches or could be manually batched and sent (WebSocket.PingAsync)
- WebSocketMessageWriteStream now can be gracefully closed with CloseAsync()
- Tested to work under load
- More unit-tests
    
Lost Features:
- UWP target platform (netstandard1.3 is added instead)
- synchronous IO methods on WebSocket and Streams (Write/Read/Close)
- Cookies collection on WebSocketHttpRequest (class is still there)
- Removed potentially dangerous feature 'RemoveBOM' on WSWriteStream.Write and refactored WriteStringAsync(), ReadStringAsync methods to use UTF-8 without BOM

Known Problems:
- WebSocketDeflateStream uses sync Stream methods, it should be rewritten to fully support async operations.
- Mono can't handle IPv6 Dual Mode sockets properly (exception in Socket.RemoteEndPoint and Socket.LocalEndPoint).
- Will not renegotiate with a WebSocket server that redirects to WSS, instead it throws an error.

### Quickstart

#### Install

[WebSocketListener is available through NuGet](https://www.nuget.org/packages/deniszykov.WebSocketListener/)

```
PM> Install-Package deniszykov.WebSocketListener
```

#### Set up
Setting up a server and start listening for clients is very similar to a `TcpListener`. An listening endpoint and a WebSocket standard is the minimum needed to set up a server.
```cs
var options = new WebSocketListenerOptions();
options.Standards.RegisterRfc6455();
var server = new WebSocketListener(new IPEndPoint(IPAddress.Any, 8006), options);
await server.StartAsync();
```
[Full Code](Samples/EchoServer/Program.cs)

The class ```vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455``` gives support to the [RFC 6455](http://tools.ietf.org/html/rfc6455), that is the WebSocket standard used at the moment. Future standards can be added in the [same way](//github.com/vtortola/WebSocketListener/wiki/Multiple-WebSocket-standard-support).

Optionally, you can also:
 * [enable TLS for secure WebSocket connections](//github.com/vtortola/WebSocketListener/wiki/Enabling-WebSocket-Secure-(TLS)).
	* `options.ConnectionExtensions.RegisterSecureConnection(certificate)`
 * [enable deflate compression for messages] (//github.com/vtortola/WebSocketListener/wiki/Deflate-extension).
	* `options.Standards.RegisterRfc6455(f=>{f.MessageExtensions.RegisterDeflateCompression();})`
 * [customize memory use, subprotocols, queuing and ping behaviours](//github.com/vtortola/WebSocketListener/wiki/WebSocketListener-options).
 * [add customized extensions](//github.com/vtortola/WebSocketListener/wiki/WebSocketListener-Extensions).


#### Accepting clients
Once the server has started, clients can be awaited asynchronously. When a client connects, a `WebSocket` object will be returned:

```cs
var webSocket = await server.AcceptWebSocketAsync(cancellation);
```

The client provides means to read and write messages. With the client, as in the underlying `NetworkStream`, is possible to write and read at the same time even from different threads, but is not possible to read from two or more threads at the same time, same for writing.

`AcceptWebSocketAsync` should be in a loop to continuously accept new clients, also wrapped in a `try/catch` since errors in the negotiation process will be thrown here. Take a look to the [simple host tutorial](https://github.com/vtortola/WebSocketListener/wiki/WebSocketListener-Echo-Server-Example).

#### Receiving messages

⚠️ You must receive messages even if you do not need them. If you do not do this, then random disconnects are possible.

⚠️ Some synchronization mechanism is required to prevent parallel reading from one instance of WebSocket. Use [SemaphoreSlim](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netframework-4.8) if you reading directly from  WebSocket. Use async while cycle and [BufferBlock](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.bufferblock-1?view=netcore-2.2) if you want to form a read message queue.

With the `webSocket` we can *await* for a message:

```cs
var messageReader = await webSocket.ReadMessageAsync(cancellationToken);
```

Messages are a stream-like objects, so is it possible to use regular .NET framework tools to work with them. The `WebSocketMessageReadStream.MessageType` property indicates the kind of content the message contains, so it can be used to select a different handling approach.

The returned `WebSocketMessageReadStream` object will contain information from the header, like type of message (Text or Binary) but not the message content, neither the message length, since a frame only contains the frame length rather than the total message length, therefore that information could be missleading.

A text message can be read with a simple `StreamReader`.  It is worth remember that according to the WebSockets specs, it always uses UTF8-no-BOM for text enconding:

```cs
var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

if(messageReader.MessageType == WebSocketMessageType.Text)
{
   String msgContent = String.Empty;
   using (var reader = new StreamReader(messageReadStream, utf8NoBom))
        msgContent = await reader.ReadToEndAsync();
}
```

```ReadMessageAsync``` should go in a loop, to read messages continuously. Writes and read can be performed at the same time. Take a look to the [simple host tutorial](//github.com/vtortola/WebSocketListener/wiki/WebSocketListener-Echo-Server-Example).

Also, a binary message can be read using regular .NET techniques:

```cs
if(messageReader.MessageType == WebSocketMessageType.Binary)
{
   using (var stream = new MemoryStream())
   {
       await messageReader.CopyToAsync(stream);
   }
}
```

#### Sending messages

⚠️ Some synchronization mechanism is required to prevent parallel writing to one instance of WebSocket. Use [SemaphoreSlim](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netframework-4.8) if you writing directly to WebSocket. Use [ActionBlock](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.actionblock-1?view=netcore-2.2) if you want to form a write message queue.

Writing messages is also easy. The `webSocket.CreateMessageWriter(WebSocketMessageType)` method allows to create a write only  stream:

```cs
using (var messageWriter = webSocket.CreateMessageWriter(WebSocketMessageType.Text))
```

Once a message writer is created, regular .NET tools can be used to write in it:

```cs
using (var messageWriter = webSocket.CreateMessageWriter(WebSocketMessageType.Text))
using (var streamWriter = new StreamWriter(messageWriter, utf8NoBom))
{
   await streamWriter.WriteAsync("Hello World!");
   await streamWriter.FlushAsync();
   await messageWriter.CloseAsync();
}
```    

Or:

```cs
webSocket.WriteStringAsync("Hello World!");
```

Also binary stream messages:

```cs
using (var messageWriter = webSocket.CreateMessageWriter(WebSocketMessageType.Binary))
{
   await myFileStream.CopyToAsync(messageWriter);
   await messageWriter.CloseAsync();    
}
```
Also binary messages:

```cs
using (var messageWriter = webSocket.CreateMessageWriter(WebSocketMessageType.Binary))
{
   await writer.WriteAndCloseAsync(bytes, offset, count).ConfigureAwait(false);
}
```

Or:

```cs
webSocket.WriteBytesAsync(bytes, offset, count);
```

#### Example
Take a look on the [WebSocketListener samples](//github.com/vtortola/WebSocketListener/wiki/WebSocketListener-Samples).

### The MIT License (MIT)

Copyright (c) 2014 vtortola, deniszykov

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

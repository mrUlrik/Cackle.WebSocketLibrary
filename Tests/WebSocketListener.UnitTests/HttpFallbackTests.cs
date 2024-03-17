using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using NUnit.Framework.Internal;
using System.Text;
using Moq;
using vtortola.WebSockets.Http;
using vtortola.WebSockets.Rfc6455;
using vtortola.WebSockets.Transports;

namespace vtortola.WebSockets.UnitTests
{
    public class HttpFallbackTests
    {
        private readonly Mock<IHttpFallback> fallback;
        private readonly List<Tuple<IHttpRequest, NetworkConnection>> postedConnections;
        private readonly WebSocketFactoryCollection factories;
        private readonly ILogger logger;

        public HttpFallbackTests()
        {
            this.logger = new TestLogger();
            this.factories = new WebSocketFactoryCollection();
            this.factories.Add(new WebSocketFactoryRfc6455());

            this.fallback = new Mock<IHttpFallback>();
            this.fallback.Setup(x => x.Post(It.IsAny<IHttpRequest>(), It.IsAny<NetworkConnection>()))
                .Callback((IHttpRequest r, NetworkConnection s) => this.postedConnections.Add(new Tuple<IHttpRequest, NetworkConnection>(r, s)));
            this.postedConnections = new List<Tuple<IHttpRequest, NetworkConnection>>();
        }

        [Test]
        public void HttpFallback()
        {
            var options = new WebSocketListenerOptions { Logger = this.logger };
            options.HttpFallback = this.fallback.Object;
            var handshaker = new WebSocketHandshaker(this.factories, options);

            using (var connectionInput = new MemoryStream())
            using (var connectionOutput = new MemoryStream())
            using (var connection = new DummyNetworkConnection(connectionInput, connectionOutput))
            {
                using (var sw = new StreamWriter(connectionInput, Encoding.ASCII, 1024, true))
                {
                    sw.WriteLine(@"GET /chat HTTP/1.1");
                    sw.WriteLine(@"Host: server.example.com");
                    sw.WriteLine(@"Cookie: key=W9g/8FLW8RAFqSCWBvB9Ag==#5962c0ace89f4f780aa2a53febf2aae5;");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.False((bool)result.IsWebSocketRequest);
                Assert.False((bool)result.IsValidWebSocketRequest);
                Assert.True((bool)result.IsValidHttpRequest);
                Assert.False((bool)result.IsVersionSupported);
                Assert.That(new Uri(result.Request.Headers[RequestHeader.Origin]), Is.EqualTo(new Uri("http://example.com")));
                Assert.That((string)result.Request.Headers[RequestHeader.Host], Is.EqualTo((string)"server.example.com"));
                Assert.That((string)result.Request.RequestUri.ToString(), Is.EqualTo((string)@"/chat"));
                Assert.That(result.Request.Cookies.Count, Is.EqualTo(1));
                var cookie = result.Request.Cookies["key"];
                Assert.That((string)cookie.Name, Is.EqualTo((string)"key"));
                Assert.That((string)cookie.Value, Is.EqualTo((string)@"W9g/8FLW8RAFqSCWBvB9Ag==#5962c0ace89f4f780aa2a53febf2aae5"));
                Assert.NotNull(result.Request.LocalEndPoint);
                Assert.NotNull(result.Request.RemoteEndPoint);
            }
        }

        [Test]
        public void SimpleHandshakeIgnoringFallback()
        {
            var options = new WebSocketListenerOptions { Logger = this.logger };
            options.HttpFallback = this.fallback.Object;
            var handshaker = new WebSocketHandshaker(this.factories, options);

            using (var connectionInput = new MemoryStream())
            using (var connectionOutput = new MemoryStream())
            using (var connection = new DummyNetworkConnection(connectionInput, connectionOutput))
            {
                using (var sw = new StreamWriter(connectionInput, Encoding.ASCII, 1024, true))
                {
                    sw.WriteLine(@"GET /chat HTTP/1.1");
                    sw.WriteLine(@"Host: server.example.com");
                    sw.WriteLine(@"Upgrade: websocket");
                    sw.WriteLine(@"Connection: Upgrade");
                    sw.WriteLine(@"Cookie: key=W9g/8FLW8RAFqSCWBvB9Ag==#5962c0ace89f4f780aa2a53febf2aae5;");
                    sw.WriteLine(@"Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.True((bool)result.IsVersionSupported);
                Assert.That(new Uri(result.Request.Headers[RequestHeader.Origin]), Is.EqualTo(new Uri("http://example.com")));
                Assert.That((string)result.Request.Headers[RequestHeader.Host], Is.EqualTo((string)"server.example.com"));
                Assert.That((string)result.Request.RequestUri.ToString(), Is.EqualTo((string)@"/chat"));
                Assert.That(result.Request.Cookies.Count, Is.EqualTo(1));
                var cookie = result.Request.Cookies["key"];
                Assert.That((string)cookie.Name, Is.EqualTo((string)"key"));
                Assert.That((string)cookie.Value, Is.EqualTo((string)@"W9g/8FLW8RAFqSCWBvB9Ag==#5962c0ace89f4f780aa2a53febf2aae5"));
                Assert.NotNull(result.Request.LocalEndPoint);
                Assert.NotNull(result.Request.RemoteEndPoint);

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 101 Switching Protocols");
                sb.AppendLine(@"Upgrade: websocket");
                sb.AppendLine(@"Connection: Upgrade");
                sb.AppendLine(@"Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }
    }
}

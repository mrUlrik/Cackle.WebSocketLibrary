using System.Net;
using NUnit.Framework.Internal;
using System.Text;
using Moq;
using vtortola.WebSockets.Http;
using vtortola.WebSockets.Rfc6455;

namespace vtortola.WebSockets.UnitTests
{
    public class WebSocketHandshakerTests
    {
        private readonly WebSocketFactoryCollection factories;

        public WebSocketHandshakerTests()
        {
            this.factories = new WebSocketFactoryCollection();
            this.factories.Add(new WebSocketFactoryRfc6455());
        }

        [Test]
        public void DetectReturnCookieErrors()
        {
            var handshaker = new WebSocketHandshaker(this.factories,
                new WebSocketListenerOptions
                {
                    Logger = new TestLogger(),
                    HttpAuthenticationHandler = (req, res) =>
                    {
                        throw new Exception("dummy");
                    }
                });

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
                Assert.False((bool)result.IsValidWebSocketRequest);
                Assert.NotNull(result.Error);

                connectionOutput.Position = 0;

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 500 Internal Server Error");
                sb.AppendLine(@"Connection: close");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void DoSimpleHandshake()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions { Logger = new TestLogger() });

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

                connectionInput.Position = 0;

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

                connectionOutput.Position = 0;

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

        [Test]
        public void DoSimpleHandshakeWithEndpoints()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions { Logger = new TestLogger() });

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

                Assert.That(result.Request.LocalEndPoint.ToString(), Is.EqualTo(connection.LocalEndPoint.ToString()));
                Assert.That(result.Request.RemoteEndPoint.ToString(), Is.EqualTo(connection.RemoteEndPoint.ToString()));
            }
        }

        [Test]
        public void DoSimpleHandshakeVerifyCaseInsensitive()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions { Logger = new TestLogger() });

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
                    sw.WriteLine(@"Sec-Websocket-Key: x3JJHMbDL1EzLkh9GBhXDw==");
                    sw.WriteLine(@"Sec-Websocket-Version: 13");
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

        [Test]
        public void IndicateANonSupportedVersion()
        {
            var extension = new Mock<IWebSocketMessageExtension>();
            extension.Setup(x => x.Name).Returns("test-extension");
            var ext = new WebSocketExtension("test-extension", new List<WebSocketExtensionOption>(new[]
            {
                new WebSocketExtensionOption("optionA")
            }));
            IWebSocketMessageExtensionContext ctx;

            extension.Setup(x => x.TryNegotiate(It.IsAny<WebSocketHttpRequest>(), out ext, out ctx))
                     .Returns(true);

            var factory = new WebSocketFactoryRfc6455();
            factory.MessageExtensions.Add(extension.Object);
            var factories = new WebSocketFactoryCollection();
            factories.Add(factory);
            var handshaker = new WebSocketHandshaker(factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Extensions: test-extension;optionA");
                    sw.WriteLine(@"Sec-WebSocket-Version: 14");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.False((bool)result.IsVersionSupported);

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 426 Upgrade Required");
                sb.AppendLine(@"Sec-WebSocket-Version: 13");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void IndicateANonWebSocketConnection()
        {
            var extension = new Mock<IWebSocketMessageExtension>();
            extension.Setup(x => x.Name).Returns("test-extension");
            var ext = new WebSocketExtension("test-extension", new List<WebSocketExtensionOption>(new[]
            {
                new WebSocketExtensionOption("optionA")
            }));
            IWebSocketMessageExtensionContext ctx;

            extension.Setup(x => x.TryNegotiate(It.IsAny<WebSocketHttpRequest>(), out ext, out ctx))
                     .Returns(true);

            var factory = new WebSocketFactoryRfc6455();
            factory.MessageExtensions.Add(extension.Object);
            var factories = new WebSocketFactoryCollection();
            factories.Add(factory);
            var handshaker = new WebSocketHandshaker(factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

            using (var connectionInput = new MemoryStream())
            using (var connectionOutput = new MemoryStream())
            using (var connection = new DummyNetworkConnection(connectionInput, connectionOutput))
            {
                using (var sw = new StreamWriter(connectionInput, Encoding.ASCII, 1024, true))
                {
                    sw.WriteLine(@"GET /chat HTTP/1.1");
                    sw.WriteLine(@"Host: server.example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.False((bool)result.IsWebSocketRequest);
                Assert.False((bool)result.IsVersionSupported);

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 400 Bad Request");
                sb.AppendLine(@"Connection: close");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void NegotiateAndIgnoreAnExtension()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Extensions: test-extension");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.That(new Uri(result.Request.Headers[RequestHeader.Origin]), Is.EqualTo(new Uri("http://example.com")));
                Assert.That((string)result.Response.Headers[ResponseHeader.WebSocketProtocol], Is.EqualTo((string)"superchat"));

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 101 Switching Protocols");
                sb.AppendLine(@"Upgrade: websocket");
                sb.AppendLine(@"Connection: Upgrade");
                sb.AppendLine(@"Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
                sb.AppendLine(@"Sec-WebSocket-Protocol: superchat");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void NegotiateAnExtension()
        {
            var extension = new Mock<IWebSocketMessageExtension>();
            extension.Setup(x => x.Name).Returns("test-extension");
            var ext = new WebSocketExtension("test-extension");
            IWebSocketMessageExtensionContext ctx;

            extension.Setup(x => x.TryNegotiate(It.IsAny<WebSocketHttpRequest>(), out ext, out ctx))
                     .Returns(true);

            var factory = new WebSocketFactoryRfc6455();
            factory.MessageExtensions.Add(extension.Object);
            var factories = new WebSocketFactoryCollection();
            factories.Add(factory);
            var handshaker = new WebSocketHandshaker(factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Extensions: test-extension");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.That(new Uri(result.Request.Headers[RequestHeader.Origin]), Is.EqualTo(new Uri("http://example.com")));
                Assert.That((string)result.Response.Headers[ResponseHeader.WebSocketProtocol], Is.EqualTo((string)"superchat"));

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 101 Switching Protocols");
                sb.AppendLine(@"Upgrade: websocket");
                sb.AppendLine(@"Connection: Upgrade");
                sb.AppendLine(@"Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
                sb.AppendLine(@"Sec-WebSocket-Protocol: superchat");
                sb.AppendLine(@"Sec-WebSocket-Extensions: test-extension");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void NegotiateAnExtensionWithParameters()
        {
            var extension = new Mock<IWebSocketMessageExtension>();
            extension.Setup(x => x.Name).Returns("test-extension");
            var ext = new WebSocketExtension("test-extension", new List<WebSocketExtensionOption>(new[]
            {
                new WebSocketExtensionOption("optionA")
            }));
            IWebSocketMessageExtensionContext ctx;

            extension.Setup(x => x.TryNegotiate(It.IsAny<WebSocketHttpRequest>(), out ext, out ctx))
                     .Returns(true);

            var factory = new WebSocketFactoryRfc6455();
            factory.MessageExtensions.Add(extension.Object);
            var factories = new WebSocketFactoryCollection();
            factories.Add(factory);
            var handshaker = new WebSocketHandshaker(factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Extensions: test-extension;optionA");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.That(new Uri(result.Request.Headers[RequestHeader.Origin]), Is.EqualTo(new Uri("http://example.com")));
                Assert.That((string)result.Response.Headers[ResponseHeader.WebSocketProtocol], Is.EqualTo((string)"superchat"));

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 101 Switching Protocols");
                sb.AppendLine(@"Upgrade: websocket");
                sb.AppendLine(@"Connection: Upgrade");
                sb.AppendLine(@"Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
                sb.AppendLine(@"Sec-WebSocket-Protocol: superchat");
                sb.AppendLine(@"Sec-WebSocket-Extensions: test-extension;optionA");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void NegotiateASubProtocol()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.That(new Uri(result.Request.Headers[RequestHeader.Origin]), Is.EqualTo(new Uri("http://example.com")));
                Assert.That((string)result.Response.Headers[ResponseHeader.WebSocketProtocol], Is.EqualTo((string)"superchat"));

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 101 Switching Protocols");
                sb.AppendLine(@"Upgrade: websocket");
                sb.AppendLine(@"Connection: Upgrade");
                sb.AppendLine(@"Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
                sb.AppendLine(@"Sec-WebSocket-Protocol: superchat");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void ParseCookies()
        {
            var parsed = CookieParser.Parse("cookie1=uno").ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(1));
            Assert.That(parsed[0].Name, Is.EqualTo("cookie1"));
            Assert.That(parsed[0].Value, Is.EqualTo("uno"));

            parsed = CookieParser.Parse("cookie1=uno;cookie2=dos").ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(2));
            Assert.That(parsed[0].Name, Is.EqualTo("cookie1"));
            Assert.That(parsed[0].Value, Is.EqualTo("uno"));
            Assert.That(parsed[1].Name, Is.EqualTo("cookie2"));
            Assert.That(parsed[1].Value, Is.EqualTo("dos"));

            parsed = CookieParser.Parse("cookie1=uno; cookie2=dos ").ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(2));
            Assert.That(parsed[0].Name, Is.EqualTo("cookie1")); 
            Assert.That(parsed[0].Value, Is.EqualTo("uno"));
            Assert.That(parsed[1].Name, Is.EqualTo("cookie2"));
            Assert.That(parsed[1].Value, Is.EqualTo("dos"));

            parsed = CookieParser.Parse("cookie1=uno; cookie2===dos== ").ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(2));
            Assert.That(parsed[0].Name, Is.EqualTo("cookie1"));
            Assert.That(parsed[0].Value, Is.EqualTo("uno"));
            Assert.That(parsed[1].Name, Is.EqualTo("cookie2"));
            Assert.That(parsed[1].Value, Is.EqualTo("==dos=="));

            parsed = CookieParser
                .Parse(
                    "language=ru; _ym_uid=1111111111111; _ym_isad=2; __test; settings=%7B%22market_730_onPage%22%3A24%7D; timezoneOffset=10800")
                .ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(6));
            Assert.That(parsed[0].Name, Is.EqualTo("language"));
            Assert.That(parsed[0].Value, Is.EqualTo("ru"));
            Assert.That(parsed[1].Name, Is.EqualTo("_ym_uid"));
            Assert.That(parsed[1].Value, Is.EqualTo("1111111111111"));
            Assert.That(parsed[2].Name, Is.EqualTo("_ym_isad"));
            Assert.That(parsed[2].Value, Is.EqualTo("2"));
            Assert.That(parsed[3].Name, Is.EqualTo("__test"));
            Assert.That(parsed[3].Value, Is.EqualTo(""));
            Assert.That(parsed[4].Name, Is.EqualTo("settings"));
            Assert.That(parsed[4].Value, Is.EqualTo("{\"market_730_onPage\":24}"));
            Assert.That(parsed[5].Name, Is.EqualTo("timezoneOffset"));
            Assert.That(parsed[5].Value, Is.EqualTo("10800"));

            parsed = CookieParser.Parse(null).ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(0));

            parsed = CookieParser.Parse(string.Empty).ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(0));

            parsed = CookieParser.Parse("   ").ToArray();
            Assert.NotNull(parsed);
            Assert.That(parsed.Length, Is.EqualTo(0));
        }

        [Test]
        public void ParseMultipleCookie()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions { Logger = new TestLogger() });

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
                    sw.WriteLine(@"Cookie: cookie1=uno; cookie2=dos");
                    sw.WriteLine(@"Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);

                Assert.That(result.Request.Cookies.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void ReturnCookies()
        {
            var handshaker = new WebSocketHandshaker(this.factories,
                new WebSocketListenerOptions
                {
                    Logger = new TestLogger(),
                    HttpAuthenticationHandler = (request, response) =>
                    {
                        response.Cookies.Add(new Cookie("name1", "value1"));
                        response.Cookies.Add(new Cookie("name2", "value2"));
                        return Task.FromResult(true);
                    }
                });

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

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 101 Switching Protocols");
                sb.AppendLine(@"Upgrade: websocket");
                sb.AppendLine(@"Connection: Upgrade");
                sb.AppendLine(@"Set-Cookie: name1=value1");
                sb.AppendLine(@"Set-Cookie: name2=value2");
                sb.AppendLine(@"Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void SendCustomErrorCode()
        {
            var handshaker = new WebSocketHandshaker(this.factories,
                new WebSocketListenerOptions
                {
                    Logger = new TestLogger(),
                    HttpAuthenticationHandler = (req, res) =>
                    {
                        res.Status = HttpStatusCode.Unauthorized;
                        return Task.FromResult(false);
                    }
                });

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
                Assert.False((bool)result.IsValidWebSocketRequest);
                Assert.NotNull(result.Error);

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 401 Unauthorized");
                sb.AppendLine(@"Connection: close");
                sb.AppendLine();

                using (var sr = new StreamReader(connectionOutput))
                {
                    var s = sr.ReadToEnd();
                    Assert.That(s, Is.EqualTo(sb.ToString()));
                }
            }
        }

        [Test]
        public void UnderstandEncodedCookies()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions { Logger = new TestLogger() });

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
                    sw.WriteLine(@"Cookie: key=This%20is%20encoded.");
                    sw.WriteLine(@"Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsValidWebSocketRequest);
                Assert.That(result.Request.Cookies.Count, Is.EqualTo(1));
                Assert.That((string)result.Request.Cookies["key"].Value, Is.EqualTo((string)"This is encoded."));
            }
        }

        [Test]
        public void DoesNotFailWhenSubProtocolRequestedButNoMatch()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat2", "text"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.True((bool)result.IsVersionSupported);
                Assert.Null(result.Error);
                Assert.True((bool)result.IsValidWebSocketRequest);
                Assert.True(string.IsNullOrEmpty(result.Response.Headers[ResponseHeader.WebSocketProtocol]));

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

        [Test]
        public void DoNotFailWhenSubProtocolRequestedButNotOffered()
        {
            var handshaker = new WebSocketHandshaker(this.factories, new WebSocketListenerOptions { Logger = new TestLogger() });

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
                    sw.WriteLine(@"Sec-WebSocket-Protocol: chat, superchat");
                    sw.WriteLine(@"Sec-WebSocket-Version: 13");
                    sw.WriteLine(@"Origin: http://example.com");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.True((bool)result.IsWebSocketRequest);
                Assert.True((bool)result.IsVersionSupported);
                Assert.Null(result.Error);
                Assert.True((bool)result.IsValidWebSocketRequest);

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

        [Test]
        public void FailWhenBadRequest()
        {
            var extension = new Mock<IWebSocketMessageExtension>();
            extension.Setup(x => x.Name).Returns("test-extension");
            var ext = new WebSocketExtension("test-extension", new List<WebSocketExtensionOption>(new[]
            {
                new WebSocketExtensionOption("optionA")
            }));
            IWebSocketMessageExtensionContext ctx;

            extension.Setup(x => x.TryNegotiate(It.IsAny<WebSocketHttpRequest>(), out ext, out ctx))
                     .Returns(true);

            var factory = new WebSocketFactoryRfc6455();
            factory.MessageExtensions.Add(extension.Object);
            var factories = new WebSocketFactoryCollection();
            factories.Add(factory);
            var handshaker = new WebSocketHandshaker(factories, new WebSocketListenerOptions
            {
                Logger = new TestLogger(),
                SubProtocols = new[]
                {
                    "superchat"
                }
            });

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
                    sw.WriteLine(@"Sec-WebSocket-Protoco");
                }

                connectionInput.Seek(0, SeekOrigin.Begin);

                var result = handshaker.HandshakeAsync(connection).Result;
                Assert.NotNull(result);
                Assert.False((bool)result.IsWebSocketRequest);
                Assert.False((bool)result.IsVersionSupported);

                connectionOutput.Seek(0, SeekOrigin.Begin);

                var sb = new StringBuilder();
                sb.AppendLine(@"HTTP/1.1 400 Bad Request");
                sb.AppendLine(@"Connection: close");
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

using System.Net;
using vtortola.WebSockets.Tools;

namespace vtortola.WebSockets.UnitTests
{
    public class HttpHelperTest
    {
        [Theory,
        TestCase("HTTP/1.1 101 Web Socket Protocol Handshake", HttpStatusCode.SwitchingProtocols, "Web Socket Protocol Handshake"),
        TestCase("HTTP/1.0 200 OK", HttpStatusCode.OK, "OK"),
        TestCase("HTTP/1.1 200 OK", HttpStatusCode.OK, "OK"),
        TestCase("HTTP/1.1 404 Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1 404 Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase("  HTTP/1.1 404 Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1  404 Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1   404 Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1   404  Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1   404   Not Found", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1   404   Not Found ", HttpStatusCode.NotFound, "Not Found"),
        TestCase(" HTTP/1.1   404   Not Found  ", HttpStatusCode.NotFound, "Not Found"),
        TestCase("HTTP/1.1 200", HttpStatusCode.OK, ""),
        TestCase("HTTP/1.1 ", (HttpStatusCode)0, "Missing Response Code"),
        TestCase("HTTP/1.1 WRONG", (HttpStatusCode)0, "Missing Response Code"),
        TestCase("HTTP/1.1", (HttpStatusCode)0, "Missing Response Code"),
        TestCase("HTTP/1", (HttpStatusCode)0, "Malformed Response"),
        TestCase("", (HttpStatusCode)0, "Malformed Response"),
        TestCase("200 OK", (HttpStatusCode)0, "Malformed Response")]
        public void TryParseAndAddRequestHeaderTest(string headline, HttpStatusCode statusCode, string description)
        {
            var actualStatusCode = default(HttpStatusCode);
            var actualDescription = default(string);
            HttpHelper.TryParseHttpResponse(headline, out actualStatusCode, out actualDescription);

            Assert.That(actualStatusCode, Is.EqualTo(statusCode));
            Assert.That(actualDescription, Is.EqualTo(description));
        }
    }
}

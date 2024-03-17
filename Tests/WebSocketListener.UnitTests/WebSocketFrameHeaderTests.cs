using System.Net.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace vtortola.WebSockets.UnitTests
{
    public class WebSocketFrameHeaderTests
    {
        [Test]
        public void CreateBigHeaderInt32()
        {
            var header = WebSocketFrameHeader.Create(int.MaxValue, true, false, 0, WebSocketFrameOption.Text, new WebSocketExtensionFlags());
            Assert.That(header.HeaderLength, Is.EqualTo(10));
            var buffer = new byte[10];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(129));
            Assert.That(buffer[1], Is.EqualTo(127));
            Assert.That(buffer[2], Is.EqualTo(0));
            Assert.That(buffer[3], Is.EqualTo(0));
            Assert.That(buffer[4], Is.EqualTo(0));
            Assert.That(buffer[5], Is.EqualTo(0));
            Assert.That(buffer[6], Is.EqualTo(127));
            Assert.That(buffer[7], Is.EqualTo(255));
            Assert.That(buffer[8], Is.EqualTo(255));
            Assert.That(buffer[9], Is.EqualTo(255));
        }

        [Test]
        public void CreateBigHeaderInt64()
        {
            var header = WebSocketFrameHeader.Create(long.MaxValue, true, false, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            var buffer = new byte[10];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(129));
            Assert.That(buffer[1], Is.EqualTo(127));
            Assert.That(buffer[2], Is.EqualTo(127));
            Assert.That(buffer[3], Is.EqualTo(255));
            Assert.That(buffer[4], Is.EqualTo(255));
            Assert.That(buffer[5], Is.EqualTo(255));
            Assert.That(buffer[6], Is.EqualTo(255));
            Assert.That(buffer[7], Is.EqualTo(255));
            Assert.That(buffer[8], Is.EqualTo(255));
            Assert.That(buffer[9], Is.EqualTo(255));
        }

        [Test]
        public void CreateBinaryFrameHeader()
        {
            var header = WebSocketFrameHeader.Create(101, true, false, 0, WebSocketFrameOption.Binary,
                new WebSocketExtensionFlags());
            var buffer = new byte[2];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(130));
            Assert.That(buffer[1], Is.EqualTo(101));
        }

        [Test]
        public void CreateBinaryFrameHeaderWithExtensions()
        {
            var header = WebSocketFrameHeader.Create(101, true, false, 0, WebSocketFrameOption.Binary,
                new WebSocketExtensionFlags
                {
                    Rsv1 = true,
                    Rsv2 = true
                });
            var buffer = new byte[2];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(226));
            Assert.That(buffer[1], Is.EqualTo(101));
        }

        [Test]
        public void CreateContinuationPartialFrameHeader()
        {
            var header = WebSocketFrameHeader.Create(101, false, true, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            var buffer = new byte[2];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(0));
            Assert.That(buffer[1], Is.EqualTo(101));
        }

        [Test]
        public void CreateFinalPartialFrameHeader()
        {
            var header = WebSocketFrameHeader.Create(101, true, true, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            var buffer = new byte[2];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(128));
            Assert.That(buffer[1], Is.EqualTo(101));
        }

        [Test]
        public void CreateMediumHeader()
        {
            var header = WebSocketFrameHeader.Create(138, true, false, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            Assert.That(header.HeaderLength, Is.EqualTo(4));
            var buffer = new byte[4];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(129));
            Assert.That(buffer[1], Is.EqualTo(126));
            Assert.That(buffer[2], Is.EqualTo(0));
            Assert.That(buffer[3], Is.EqualTo(138));
        }

        [Test]
        public void CreateMediumHeaderBiggerThanInt16()
        {
            ushort ilength = (ushort)short.MaxValue + 1;

            var header = WebSocketFrameHeader.Create(ilength, true, false, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            Assert.That(header.HeaderLength, Is.EqualTo(4));
            var buffer = new byte[4];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(129));
            Assert.That(buffer[1], Is.EqualTo(126));
            Assert.That(buffer[2], Is.EqualTo(128));
            Assert.That(buffer[3], Is.EqualTo(0));
        }

        [Test]
        public void CreateMediumMaxHeader()
        {
            var header = WebSocketFrameHeader.Create(ushort.MaxValue, true, false, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            Assert.That(header.HeaderLength, Is.EqualTo(4));
            var buffer = new byte[4];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(129));
            Assert.That(buffer[1], Is.EqualTo(126));
            Assert.That(buffer[2], Is.EqualTo(255));
            Assert.That(buffer[3], Is.EqualTo(255));
        }
        [Test]
        public void CreateSmallHeader()
        {
            var header = WebSocketFrameHeader.Create(101, true, false, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            var buffer = new byte[2];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(129));
            Assert.That(buffer[1], Is.EqualTo(101));
        }

        [Test]
        public void CreateStartPartialFrameHeader()
        {
            var header = WebSocketFrameHeader.Create(101, false, false, 0, WebSocketFrameOption.Text,
                new WebSocketExtensionFlags());
            Assert.That(header.HeaderLength, Is.EqualTo(2));
            var buffer = new byte[2];
            header.WriteTo(buffer, 0);
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(101));
        }

        [Test]
        public void ParseBigHeader()
        {
            var buffer = new byte[10];
            buffer[0] = 129;
            buffer[1] = 127;

            var length = BitConverter.GetBytes(long.MaxValue);
            Array.Reverse(length);
            length.CopyTo(buffer, 2);

            WebSocketFrameHeader header;
            Assert.True(WebSocketFrameHeader.TryParse(buffer, 0, 10, out header));
            Assert.NotNull(header);
            Assert.True(header.Flags.FIN);
            Assert.False(header.Flags.MASK);
            Assert.That(header.HeaderLength, Is.EqualTo(10));
            Assert.That(header.ContentLength, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void ParseMediumHeader()
        {
            var buffer = new byte[6];
            buffer[0] = 129;
            buffer[1] = 126;

            ushort ilength = (ushort)short.MaxValue + 1;
            var length = BitConverter.GetBytes(ilength);
            Array.Reverse(length);
            length.CopyTo(buffer, 2);

            WebSocketFrameHeader header;
            Assert.True(WebSocketFrameHeader.TryParse(buffer, 0, 4, out header));
            Assert.NotNull(header);
            Assert.True(header.Flags.FIN);
            Assert.False(header.Flags.MASK);
            Assert.That(header.HeaderLength, Is.EqualTo(4));
            Assert.That(header.ContentLength, Is.EqualTo(ilength));
        }

        [Test]
        public void ParseMediumMaxHeader()
        {
            var buffer = new byte[6];
            buffer[0] = 129;
            buffer[1] = 126;

            var ilength = ushort.MaxValue;
            var length = BitConverter.GetBytes(ilength);
            Array.Reverse(length);
            length.CopyTo(buffer, 2);

            WebSocketFrameHeader header;
            Assert.True(WebSocketFrameHeader.TryParse(buffer, 0, 4, out header));
            Assert.NotNull(header);
            Assert.True(header.Flags.FIN);
            Assert.False(header.Flags.MASK);
            Assert.That(header.HeaderLength, Is.EqualTo(4));
            Assert.That(header.ContentLength, Is.EqualTo(ilength));
        }

        [Test]
        public void ParseSmallHeader()
        {
            var buffer = new byte[6];
            buffer[0] = 129;
            buffer[1] = 101;

            WebSocketFrameHeader header;
            Assert.True(WebSocketFrameHeader.TryParse(buffer, 0, 2, out header));
            Assert.NotNull(header);
            Assert.True(header.Flags.FIN);
            Assert.False(header.Flags.MASK);
            Assert.That(header.HeaderLength, Is.EqualTo(2));
            Assert.That(header.ContentLength, Is.EqualTo(101));
        }

        [Test]
        public void FailToParseBigHeaderWhenOverflowsInt64()
        {
            var buffer = new byte[10];
            buffer[0] = 129;
            buffer[1] = 127;

            var ilength = (ulong)long.MaxValue + 1;
            var length = BitConverter.GetBytes(ilength);
            Array.Reverse(length);
            length.CopyTo(buffer, 2);

            Assert.Throws<WebSocketException>(() =>
            {
                WebSocketFrameHeader header;
                Assert.True(WebSocketFrameHeader.TryParse(buffer, 0, 10, out header));
                Assert.That(header.HeaderLength, Is.EqualTo(10));
            });
        }
    }
}

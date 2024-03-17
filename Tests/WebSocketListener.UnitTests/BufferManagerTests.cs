namespace vtortola.WebSockets.UnitTests
{
    public class BufferManagerTests
    {
        [Theory,
        TestCase(1),
        TestCase(2),
        TestCase(8),
        TestCase(64),
        TestCase(256),
        TestCase(333),
        TestCase(800),
        TestCase(1024),
        TestCase(2047),
        TestCase(2048),
        TestCase(2049)]
        public void TakeBuffer(int maxBufferSize)
        {
            var bufferManager = BufferManager.CreateBufferManager(maxBufferSize * 100, maxBufferSize);
            var buffer = bufferManager.TakeBuffer(maxBufferSize);

            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= maxBufferSize, "buffer.Length >= maxBufferSize");
        }

        [Theory,
        TestCase(1024, 1),
        TestCase(1024, 2),
        TestCase(1024, 8),
        TestCase(1024, 64),
        TestCase(1024, 256),
        TestCase(1024, 333),
        TestCase(1024, 800),
        TestCase(4096, 1),
        TestCase(4096, 2),
        TestCase(4096, 8),
        TestCase(4096, 64),
        TestCase(4096, 256),
        TestCase(4096, 333),
        TestCase(4096, 800),
        TestCase(4096, 1024),
        TestCase(4096, 2047),
        TestCase(4096, 2048),
        TestCase(4096, 2049)]
        public void TakeSmallBuffer(int maxBufferSize, int takeBufferSize)
        {
            var bufferManager = BufferManager.CreateBufferManager(maxBufferSize * 100, maxBufferSize);
            var buffer = bufferManager.TakeBuffer(takeBufferSize);

            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= takeBufferSize, "buffer.Length >= maxBufferSize");
        }

        [Theory,
        TestCase(1),
        TestCase(2),
        TestCase(8),
        TestCase(64),
        TestCase(256),
        TestCase(333),
        TestCase(800),
        TestCase(1024),
        TestCase(2047),
        TestCase(2048),
        TestCase(2049)]
        public void ReturnBuffer(int maxBufferSize)
        {
            var bufferManager = BufferManager.CreateBufferManager(maxBufferSize * 100, maxBufferSize);
            var buffer = bufferManager.TakeBuffer(maxBufferSize);

            Assert.NotNull(buffer);

            bufferManager.ReturnBuffer(buffer);
        }

        [Theory,
        TestCase(1024, 1),
        TestCase(1024, 2),
        TestCase(1024, 8),
        TestCase(1024, 64),
        TestCase(1024, 256),
        TestCase(1024, 333),
        TestCase(1024, 800),
        TestCase(4096, 1),
        TestCase(4096, 2),
        TestCase(4096, 8),
        TestCase(4096, 64),
        TestCase(4096, 256),
        TestCase(4096, 333),
        TestCase(4096, 800),
        TestCase(4096, 1024),
        TestCase(4096, 2047),
        TestCase(4096, 2048),
        TestCase(4096, 2049)]
        public void ReturnSmallBuffer(int maxBufferSize, int takeBufferSize)
        {
            var bufferManager = BufferManager.CreateBufferManager(maxBufferSize * 100, maxBufferSize);
            var buffer = bufferManager.TakeBuffer(takeBufferSize);

            Assert.NotNull(buffer);

            bufferManager.ReturnBuffer(buffer);
        }
        [Test]
        public void Construct()
        {
            var bufferManager = BufferManager.CreateBufferManager(1, 1);

            Assert.NotNull(bufferManager);
        }

        [Test]
        public void ConstructWithInvalidFirstParameter()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BufferManager.CreateBufferManager(-1, 1));
        }

        [Test]
        public void ConstructWithInvalidSecondParameters()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BufferManager.CreateBufferManager(1, -1));
        }
    }
}

using vtortola.WebSockets.Tools;

namespace vtortola.WebSockets
{
    internal sealed class DefaultBufferManager : BufferManager
    {
        private readonly ObjectPool<byte[]> smallPool;
        private readonly ObjectPool<byte[]> largePool;

        public override int LargeBufferSize { get; }

        public DefaultBufferManager(int smallBufferSize, int smallPoolSizeLimit, int largeBufferSize, int largePoolSizeLimit)
        {
            this.LargeBufferSize = largeBufferSize;
            this.smallPool = new ObjectPool<byte[]>(() => new byte[smallBufferSize], smallPoolSizeLimit);
            this.largePool = new ObjectPool<byte[]>(() => new byte[largeBufferSize], largePoolSizeLimit);

        }

        /// <inheritdoc/>
        public override void ReturnBuffer(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            this.largePool.Return(buffer);
        }
        /// <inheritdoc/>
        public override byte[] TakeBuffer(int bufferSize)
        {
            if (bufferSize < 0 || bufferSize > this.LargeBufferSize) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return this.largePool.Take();
        }
    }
}
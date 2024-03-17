using vtortola.WebSockets.Tools;

namespace vtortola.WebSockets
{
    public abstract class WebSocketMessageStream : Stream
    {
        public override bool CanRead => false;
        public sealed override bool CanSeek => false;
        public override bool CanWrite => false;
        public sealed override long Length { get { throw new NotSupportedException(); } }
        public sealed override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public abstract WebSocketListenerOptions Options { get; }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return TaskHelper.CompletedTask;
        }
        public abstract Task CloseAsync();
        public abstract override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        public abstract override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public sealed override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete("Do not use synchronous IO operation on network streams. Use ReadAsync() instead.")]
        public sealed override int ReadByte()
        {
            throw new NotSupportedException();
        }
        [Obsolete("Do not use synchronous IO operation on network streams. Use ReadAsync() instead.")]
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadAsync(buffer, offset, count, CancellationToken.None).Result;
        }
        [Obsolete("Do not use synchronous IO operation on network streams. Use WriteAsync() instead.")]
        public sealed override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Do not use synchronous IO operation on network streams. Use WriteAsync() instead.")]
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.WriteAsync(buffer, offset, count).Wait();
        }
        [Obsolete("Do not use synchronous IO operation on network streams. Use FlushAsync() instead.")]
        public override void Flush()
        {

        }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    }
}

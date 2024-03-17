namespace vtortola.WebSockets
{
    public abstract class WebSocketMessageReadStream : WebSocketMessageStream
    {
        public abstract WebSocketMessageType MessageType { get; }
        public abstract WebSocketExtensionFlags Flags { get; }
        public sealed override bool CanRead => true;

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        /// <inheritdoc />
        [Obsolete("Writing to the read stream is not allowed", true)]
        public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    }
}
using JetBrains.Annotations;

namespace vtortola.WebSockets
{
    public abstract class WebSocketMessageWriteStream : WebSocketMessageStream
    {
        public sealed override bool CanWrite => true;

        [NotNull]
        public WebSocketExtensionFlags ExtensionFlags { get; }

        protected WebSocketMessageWriteStream()
        {
            this.ExtensionFlags = new WebSocketExtensionFlags();
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        /// <inheritdoc />
        [Obsolete("Reading from the write stream is not allowed", true)]
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

        public abstract Task WriteAndCloseAsync([NotNull] byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
﻿/*
	Copyright (c) 2017 Denis Zykov
	License: https://opensource.org/licenses/MIT
*/

using System.Net;
using System.Net.Sockets;
using vtortola.WebSockets.Transports.Sockets;

namespace vtortola.WebSockets.Transports.Tcp
{
    internal sealed class TcpListener : SocketListener
    {
        private readonly TcpTransport transport;

        /// <inheritdoc />
        public TcpListener(TcpTransport transport, EndPoint[] endPointsToListen, WebSocketListenerOptions options)
            : base(transport, endPointsToListen, ProtocolType.Tcp, options)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (endPointsToListen == null) throw new ArgumentNullException(nameof(endPointsToListen));
            if (options == null) throw new ArgumentNullException(nameof(options));

            this.transport = transport;
        }

        /// <inheritdoc />
        protected override NetworkConnection CreateConnection(Socket socket, EndPoint localEndPoint)
        {
            if (this.transport.LingerState != null)
                socket.LingerState = this.transport.LingerState;
            socket.NoDelay = this.transport.NoDelay;
            socket.ReceiveBufferSize = this.transport.ReceiveBufferSize;
            socket.ReceiveTimeout = (int)this.transport.ReceiveTimeout.TotalMilliseconds + 1;
            socket.SendBufferSize = this.transport.SendBufferSize;
            socket.SendTimeout = (int)this.transport.SendTimeout.TotalMilliseconds + 1;
            return new TcpConnection(socket, localEndPoint);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // ReSharper disable once CoVariantArrayConversion
            return $"{nameof(TcpListener)}, {string.Join(", ", this.LocalEndpoints)}";
        }
    }
}
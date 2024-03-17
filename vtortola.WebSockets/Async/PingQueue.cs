﻿/*
	Copyright (c) 2017 Denis Zykov
	License: https://opensource.org/licenses/MIT
*/

using vtortola.WebSockets.Tools;
using PingSubscriptionList = System.Collections.Concurrent.ConcurrentBag<vtortola.WebSockets.WebSocket>;

namespace vtortola.WebSockets.Async
{
    internal class PingQueue : NotificationQueue<PingSubscriptionList>
    {
        private readonly ObjectPool<PingSubscriptionList> listPool;

        /// <inheritdoc />
        public PingQueue(TimeSpan period) : base(period)
        {
            this.listPool = new ObjectPool<PingSubscriptionList>(() => new PingSubscriptionList(), 20);
        }

        /// <inheritdoc />
        protected override PingSubscriptionList CreateSubscriptionList()
        {
            return this.listPool.Take();
        }
        /// <inheritdoc />
        protected override async void NotifySubscribers(PingSubscriptionList subscriptionList)
        {
            var webSocket = default(WebSocket);
            while (subscriptionList.TryTake(out webSocket))
            {
                if (!webSocket.IsConnected)
                    continue;

                try
                {
                    await webSocket.SendPingAsync(null, 0, 0).ConfigureAwait(false);
                }
                catch (Exception pingError)
                {
                    if (webSocket.IsConnected && pingError is ObjectDisposedException == false && pingError is ThreadAbortException == false)
                        DebugLogger.Instance.Warning("An error occurred while sending ping.", pingError);
                }

                if (webSocket.IsConnected && this.IsDisposed == false)
                    this.GetSubscriptionList().Add(webSocket);
            }
            this.listPool.Return(subscriptionList);
        }
    }


}
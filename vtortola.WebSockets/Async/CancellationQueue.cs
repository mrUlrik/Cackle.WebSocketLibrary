﻿/*
	Copyright (c) 2017 Denis Zykov
	License: https://opensource.org/licenses/MIT
*/

namespace vtortola.WebSockets.Async
{
    public class CancellationQueue : NotificationQueue<CancellationTokenSource>
    {
        public CancellationQueue(TimeSpan period)
            : base(period) { }

        protected override CancellationTokenSource CreateSubscriptionList()
        {
            return new CancellationTokenSource();
        }

        protected override void NotifySubscribers(CancellationTokenSource subscriptionList)
        {
            try
            {
                subscriptionList.Cancel(throwOnFirstException: true);
            }
            catch (Exception cancelError) when (cancelError is ThreadAbortException == false)
            {
                DebugLogger.Instance.Warning("An error occurred while canceling token on source.", cancelError);
            }
        }

    }
}

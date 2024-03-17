using System.Diagnostics;
using vtortola.WebSockets.Async;

namespace vtortola.WebSockets.UnitTests
{
    public sealed class TimedQueueTests
    {
        [Theory]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(2000)]
        [TestCase(3000)]
        public void SubscribeAndDispatch(int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            var timedQueue = new CancellationQueue(TimeSpan.FromMilliseconds(milliseconds / 2.0));
            var subscriptions = 0;
            var hits = 0;
            while (sw.ElapsedMilliseconds < milliseconds)
            {
                timedQueue.GetSubscriptionList().Token.Register(() => Interlocked.Increment(ref hits));
                subscriptions++;
                Thread.Sleep(10);
            }

            sw.Reset();
            while (sw.ElapsedMilliseconds < milliseconds && subscriptions != hits)
                Thread.Sleep(10);

            TestContext.Out.WriteLine($"[TEST] subscriptions: {subscriptions}, hits: {hits}.");

            Assert.That(hits, Is.EqualTo(subscriptions));
        }
    }
}
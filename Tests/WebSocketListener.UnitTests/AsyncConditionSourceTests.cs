using System.Diagnostics;
using vtortola.WebSockets.Async;

namespace vtortola.WebSockets.UnitTests
{
    public sealed class AsyncConditionSourceTests
    {
        [Theory]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(40)]
        [TestCase(80)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task ParallelSetContinuationTest(int subscribers)
        {
            var condition = new AsyncConditionSource();

            var hits = 0;
            var parallelLoopResult = Parallel.For(0, subscribers, i =>
            {
                if (i == subscribers / 2)
                    condition.Set();
                condition.GetAwaiter().OnCompleted(() => Interlocked.Increment(ref hits));
            });

            while (parallelLoopResult.IsCompleted == false)
                await Task.Delay(10).ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 1000 && subscribers != hits)
                Thread.Sleep(10);

            TestContext.Out.WriteLine($"[TEST] subscribers: {subscribers}, hits: {hits}.");

            Assert.That(hits, Is.EqualTo(subscribers));
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(40)]
        [TestCase(80)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task BeforeSetContinuationTest(int subscribers)
        {
            var condition = new AsyncConditionSource(isSet: true);

            var hits = 0;
            var parallelLoopResult = Parallel.For(0, subscribers, i =>
            {
                condition.GetAwaiter().OnCompleted(() => Interlocked.Increment(ref hits));
            });

            while (parallelLoopResult.IsCompleted == false)
                await Task.Delay(10).ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 1000 && subscribers != hits)
                Thread.Sleep(10);

            TestContext.Out.WriteLine($"[TEST] subscribers: {subscribers}, hits: {hits}.");

            Assert.That(hits, Is.EqualTo(subscribers));
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(40)]
        [TestCase(80)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task AfterSetContinuationTest(int subscribers)
        {
            var condition = new AsyncConditionSource(isSet: false);

            var hits = 0;
            var parallelLoopResult = Parallel.For(0, subscribers, i =>
            {
                condition.GetAwaiter().OnCompleted(() => Interlocked.Increment(ref hits));
            });

            while (parallelLoopResult.IsCompleted == false)
                await Task.Delay(10).ConfigureAwait(false);

            condition.Set();

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 1000 && subscribers != hits)
                Thread.Sleep(10);

            TestContext.Out.WriteLine($"[TEST] subscribers: {subscribers}, hits: {hits}.");

            Assert.That(hits, Is.EqualTo(subscribers));
        }
    }
}

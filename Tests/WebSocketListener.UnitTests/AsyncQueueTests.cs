using System.Collections.Concurrent;
using vtortola.WebSockets.Async;
using vtortola.WebSockets.Tools;

namespace vtortola.WebSockets.UnitTests
{
    public sealed class AsyncQueueTests
    {
        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(1000)]
        public void TryEnqueueAndTryDequeue(int count)
        {
            var asyncQueue = new AsyncQueue<int>();
            for (var i = 0; i < count; i++)
                Assert.True(asyncQueue.TryEnqueue(i), "fail to send");

            for (var i = 0; i < count; i++)
            {
                var value = default(int);
                Assert.True(asyncQueue.TryDequeue(out value), "fail to receive");
                Assert.That(value, Is.EqualTo(i));
            }

            Assert.That(asyncQueue.Count, Is.EqualTo(0));
        }

        [Theory]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(1000)]
        public void ParallelSendAndTryDequeue(int count)
        {
            var asyncQueue = new AsyncQueue<int>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, TaskScheduler = TaskScheduler.Default };
            var items = Enumerable.Range(0, count).ToArray();
            var expectedSum = items.Sum();
            Parallel.For(0, count, options, i => Assert.True(asyncQueue.TryEnqueue(i), "fail to send"));

            var actualSum = 0;
            var value = default(int);
            while (asyncQueue.TryDequeue(out value))
                actualSum += value;

            Assert.That(actualSum, Is.EqualTo(expectedSum));
            Assert.That(asyncQueue.Count, Is.EqualTo(0));
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task TryEnqueueAndDequeueAsync(int count)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
            var asyncQueue = new AsyncQueue<int>();
            var items = Enumerable.Range(0, count).ToArray();
            var expectedSum = items.Sum();

            var actualSum = 0;
            var ct = 0;
            var receiveTask = new Func<Task>(async () =>
            {
                await Task.Yield();

                while (cancellation.IsCancellationRequested == false)
                {
                    var receiveValueTask = asyncQueue.DequeueAsync(cancellation.Token).ConfigureAwait(false);
                    var value1 = await receiveValueTask;
                    var value2 = await receiveValueTask;

                    Assert.That(value2, Is.EqualTo(value1)); // check if awaited values are same

                    TestContext.Out.WriteLine(value1.ToString());
                    Interlocked.Add(ref actualSum, value1);
                    if (Interlocked.Increment(ref ct) == count)
                        return;
                }
            })();

            await Task.Delay(10, cancellation.Token).ConfigureAwait(false);

            for (var i = 0; i < count; i++)
                Assert.True(asyncQueue.TryEnqueue(i), "fail to send");

            await receiveTask.ConfigureAwait(false);

            Assert.That(actualSum, Is.EqualTo(expectedSum));
            Assert.That(asyncQueue.Count, Is.EqualTo(0));
        }

        [Theory]
        [TestCase(2)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task ParallelSendAndDequeueAsync(int count)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, TaskScheduler = TaskScheduler.Default };
            var asyncQueue = new AsyncQueue<int>();
            var items = Enumerable.Range(0, count).ToArray();
            var expectedSum = items.Sum();

            var actualSum = 0;
            var ct = 0;
            var receiveTask = new Func<Task>(async () =>
            {
                await Task.Yield();

                while (cancellation.IsCancellationRequested == false)
                {
                    var value = await asyncQueue.DequeueAsync(cancellation.Token).ConfigureAwait(false);

                    Interlocked.Add(ref actualSum, value);
                    if (Interlocked.Increment(ref ct) == count)
                        return;
                }
            })();

            Parallel.For(0, count, options, i => Assert.True(asyncQueue.TryEnqueue(i), "fail to send"));

            await receiveTask.ConfigureAwait(false);

            Assert.That(actualSum, Is.EqualTo(expectedSum));
            Assert.That(asyncQueue.Count, Is.EqualTo(0));
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public async Task BoundedInfiniteSendAndDequeueAsync(int seconds)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            var asyncQueue = new AsyncQueue<int>(10);
            var expectedValue = (int)(DateTime.Now.Ticks % int.MaxValue);

            var ct = 0;
            var receiveTask = new Func<Task>(async () =>
            {
                await Task.Yield();

                while (cancellation.IsCancellationRequested == false)
                {
                    var actual = await asyncQueue.DequeueAsync(cancellation.Token).ConfigureAwait(false);
                    ct++;
                    Assert.That(actual, Is.EqualTo(expectedValue));
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            var sendTask = new Func<Task>(async () =>
            {
                await Task.Yield();
                while (cancellation.IsCancellationRequested == false)
                {
                    asyncQueue.TryEnqueue(expectedValue);
                    await Task.Delay(10).ConfigureAwait(false);
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            while (cancellation.IsCancellationRequested == false)
                await Task.Delay(10).ConfigureAwait(false);

            await receiveTask;
            await sendTask;

            Assert.That(ct, Is.Not.EqualTo(0));
        }


        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public async Task FastSendAndSlowDequeueAsync(int seconds)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            var asyncQueue = new AsyncQueue<int>(10);
            var expectedValue = (int)(DateTime.Now.Ticks % int.MaxValue);

            var ct = 0;
            var receiveTask = new Func<Task>(async () =>
            {
                await Task.Yield();

                while (cancellation.IsCancellationRequested == false)
                {
                    await Task.Delay(2).ConfigureAwait(false);
                    var actual = await asyncQueue.DequeueAsync(cancellation.Token).ConfigureAwait(false);
                    ct++;
                    Assert.That(actual, Is.EqualTo(expectedValue));
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            var sendTask = new Func<Task>(async () =>
            {
                await Task.Yield();
                while (cancellation.IsCancellationRequested == false)
                {
                    asyncQueue.TryEnqueue(expectedValue);
                    await Task.Delay(1).ConfigureAwait(false);
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            while (cancellation.IsCancellationRequested == false)
                await Task.Delay(10).ConfigureAwait(false);

            await receiveTask;
            await sendTask;

            Assert.That(ct, Is.Not.EqualTo(0));
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public async Task SlowSendAndFastDequeueAsync(int seconds)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var asyncQueue = new AsyncQueue<int>(10);
            var expectedValue = (int)(DateTime.Now.Ticks % int.MaxValue);

            var ct = 0;
            var receiveTask = new Func<Task>(async () =>
            {
                await Task.Yield();

                while (cancellation.IsCancellationRequested == false)
                {
                    await Task.Delay(1).ConfigureAwait(false);
                    var actual = await asyncQueue.DequeueAsync(cancellation.Token).ConfigureAwait(false);
                    ct++;
                    Assert.That(actual, Is.EqualTo(expectedValue));
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            var sendTask = new Func<Task>(async () =>
            {
                await Task.Yield();
                while (cancellation.IsCancellationRequested == false)
                {
                    asyncQueue.TryEnqueue(expectedValue);
                    await Task.Delay(2).ConfigureAwait(false);
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            while (cancellation.IsCancellationRequested == false)
                await Task.Delay(10).ConfigureAwait(false);

            await receiveTask;
            await sendTask;

            Assert.That(ct, Is.Not.EqualTo(0));
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task AsyncSendAndClose(int count)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var asyncQueue = new AsyncQueue<int>();

            var sendValue = 0;
            var sendTask = new Func<Task>(async () =>
            {

                await Task.Yield();
                while (cancellation.IsCancellationRequested == false)
                {
                    if (asyncQueue.TryEnqueue(sendValue++) == false)
                        return;
                    await Task.Delay(10).ConfigureAwait(false);
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            await Task.Delay(count);
            asyncQueue.ClearAndClose();

            await sendTask;

            var value = default(int);
            var actualSum = 0;
            while (asyncQueue.TryDequeue(out value))
                actualSum++;
            var expectedSum = Enumerable.Range(0, sendValue).Sum();

            Assert.That(actualSum, Is.Not.EqualTo(expectedSum));
            Assert.That(asyncQueue.Count, Is.Not.EqualTo(0));
            cancellation.Cancel();
        }

        [Theory]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task AsyncSendAndCloseAndReceiveAll(int count)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var asyncQueue = new AsyncQueue<int>();

            var sendValue = 0;
            var sendTask = new Func<Task>(async () =>
            {

                await Task.Yield();
                while (cancellation.IsCancellationRequested == false)
                {
                    if (asyncQueue.TryEnqueue(sendValue++) == false)
                        return;
                    await Task.Delay(10).ConfigureAwait(false);
                }
            })().IgnoreFaultOrCancellation().ConfigureAwait(false);

            await Task.Delay(count);
            var actualSum = asyncQueue.TakeAllAndClose().Sum();

            await sendTask;

            var expectedSum = Enumerable.Range(0, sendValue).Sum();

            Assert.That(actualSum, Is.Not.EqualTo(expectedSum));
            Assert.That(asyncQueue.Count, Is.EqualTo(0));
            cancellation.Cancel();
        }

        [Test]
        public Task DequeueAsyncCancellation()
        {
            var asyncQueue = new AsyncQueue<int>();
            var cancellation = new CancellationTokenSource();

            var receiveAsync = asyncQueue.DequeueAsync(cancellation.Token);
            cancellation.CancelAfter(10);

            var timeout = Task.Delay(1000);
            var recvTask = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await receiveAsync.ConfigureAwait(false);
            });

            while (!receiveAsync.IsCompleted)
                if (timeout.IsCompleted)
                    throw new TimeoutException();

            Assert.That(asyncQueue.Count, Is.EqualTo(0));

            return Task.CompletedTask;
        }

        [Test]
        public Task DequeueAsyncCloseCancellation()
        {
            var asyncQueue = new AsyncQueue<int>();
            var cancellation = new CancellationTokenSource(2000);
            var receiveAsync = asyncQueue.DequeueAsync(cancellation.Token);

            var timeout = Task.Delay(1000);
            var recvTask = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await receiveAsync.ConfigureAwait(false);
            });

            asyncQueue.ClearAndClose(new OperationCanceledException());

            while (!receiveAsync.IsCompleted)
                if (timeout.IsCompleted)
                    throw new TimeoutException();

            Assert.That(asyncQueue.Count, Is.EqualTo(0));

            return Task.CompletedTask;
        }

        [Test]
        public Task DequeueAsyncCloseError()
        {
            var asyncQueue = new AsyncQueue<int>();
            var cancellation = new CancellationTokenSource(2000);
            var receiveAsync = asyncQueue.DequeueAsync(cancellation.Token);

            var timeout = Task.Delay(1000);

            try
            {
                receiveAsync.ConfigureAwait(false);
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected exception of type: {ex.GetType()} but was {ex.GetType()} instead");
            }

            asyncQueue.ClearAndClose(new IOException());

            while (!receiveAsync.IsCompleted)
                if (timeout.IsCompleted)
                    throw new TimeoutException();

            Assert.That(asyncQueue.Count, Is.EqualTo(0));

            return Task.CompletedTask;
        }

        [Test]
        public Task DequeueAsyncCloseReceiveAllCancellation()
        {
            var asyncQueue = new AsyncQueue<int>();
            var cancellation = new CancellationTokenSource(2000);
            var receiveAsync = asyncQueue.DequeueAsync(cancellation.Token);

            var timeout = Task.Delay(1000);
            var recvTask = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await receiveAsync.ConfigureAwait(false);
            });

            var all = asyncQueue.TakeAllAndClose(closeError: new OperationCanceledException());

            while (!receiveAsync.IsCompleted)
                if (timeout.IsCompleted)
                    throw new TimeoutException();

            Assert.IsEmpty(all);
            Assert.That(asyncQueue.Count, Is.EqualTo(0));

            return Task.CompletedTask;
        }

        [Theory]
        [TestCase(80000)]
        [TestCase(100000)]
        [TestCase(120000)]
        [TestCase(150000)]
        public async Task ParallelSendAndCloseReceiveAll(int count)
        {
            var cancellationSource = new CancellationTokenSource();
            var asyncQueue = new AsyncQueue<int>();
            var options = new ParallelOptions { CancellationToken = cancellationSource.Token, MaxDegreeOfParallelism = Environment.ProcessorCount / 2, TaskScheduler = TaskScheduler.Default };
            var items = new ConcurrentQueue<int>(Enumerable.Range(0, count));

            var sendTask = Task.Factory.StartNew(() => Parallel.For(0, count, options, i =>
            {
                var item = default(int);
                if (items.TryDequeue(out item))
                    if (asyncQueue.TryEnqueue(item) == false)
                        items.Enqueue(item);
            }));

            await Task.Delay(1).ConfigureAwait(false);

            var itemsInAsyncQueue = asyncQueue.TakeAllAndClose(); // deny TryEnqueue
            cancellationSource.Cancel(); // stop parallel for

            await sendTask.IgnoreFaultOrCancellation().ConfigureAwait(false);

            var actualCount = items.Count + itemsInAsyncQueue.Count;

            TestContext.Out.WriteLine($"[TEST] en-queued: {itemsInAsyncQueue.Count}, total: {count}");

            Assert.That(actualCount, Is.EqualTo(count));
            Assert.That(asyncQueue.Count, Is.EqualTo(0));
        }
    }
}

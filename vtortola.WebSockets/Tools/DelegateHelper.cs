/*
	Copyright (c) 2017 Denis Zykov
	License: https://opensource.org/licenses/MIT
*/

using System.Reflection;
using System.Security;

namespace vtortola.WebSockets.Tools
{
    internal static class DelegateHelper
    {
        private static readonly SendOrPostCallback SendOrPostCallbackRunAction;

        static DelegateHelper()
        {
            SendOrPostCallbackRunAction = RunAction;
        }

        public static void InterlockedCombine<DelegateT>(ref DelegateT location, DelegateT value) where DelegateT : class
        {
            var spinWait = new SpinWait();
            var currentValue = Volatile.Read(ref location);
            var expectedValue = default(DelegateT);

            do
            {
                expectedValue = currentValue;
                var newValue = (DelegateT)(object)Delegate.Combine((Delegate)(object)currentValue, (Delegate)(object)value);
                currentValue = Interlocked.CompareExchange(ref location, newValue, expectedValue);

                spinWait.SpinOnce();
            } while (currentValue != expectedValue);
        }
        public static bool InterlockedRemove<DelegateT>(ref DelegateT location, DelegateT value) where DelegateT : class
        {
            var currentValue = Volatile.Read(ref location);
            var expectedValue = default(DelegateT);

            if (currentValue == null) return false;

            var spinWait = new SpinWait();
            do
            {
                expectedValue = currentValue;

                var newValue = (DelegateT)(object)Delegate.Remove((Delegate)(object)currentValue, (Delegate)(object)value);
                if (newValue == currentValue) return false;

                currentValue = Interlocked.CompareExchange(ref location, newValue, expectedValue);

                spinWait.SpinOnce();
            } while (currentValue != expectedValue);

            return true;
        }

        [SecurityCritical]
        internal static void UnsafeQueueContinuation(Action continuation, bool continueOnCapturedContext, bool schedule)
        {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));

            var currentScheduler = TaskScheduler.Current ?? TaskScheduler.Default;
            var syncContext = SynchronizationContext.Current;
            var isDefaultSyncContext = syncContext == null || syncContext.GetType() == typeof(SynchronizationContext);
            if (schedule && continueOnCapturedContext && syncContext != null && !isDefaultSyncContext)
            {
                syncContext.Post(SendOrPostCallbackRunAction, continuation);
            }
            else if (schedule || currentScheduler != TaskScheduler.Default)
            {
                Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.PreferFairness, currentScheduler);
            }
            else
            {
                continuation();
            }
        }
        [SecuritySafeCritical]
        internal static void QueueContinuation(Action continuation, bool continueOnCapturedContext, bool schedule)
        {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));

            var currentScheduler = TaskScheduler.Current ?? TaskScheduler.Default;
            var syncContext = SynchronizationContext.Current;
            var isDefaultSyncContext = syncContext == null || syncContext.GetType() == typeof(SynchronizationContext);
            if (schedule && continueOnCapturedContext && syncContext != null && !isDefaultSyncContext)
            {
                syncContext.Post(SendOrPostCallbackRunAction, continuation);
            }
            else if (schedule || currentScheduler != TaskScheduler.Default)
            {
                Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.PreferFairness, currentScheduler);
            }
            else
            {
                continuation();
            }
        }

        private static void RunAction(object state)
        {
            ((Action)state)();
        }
    }
}
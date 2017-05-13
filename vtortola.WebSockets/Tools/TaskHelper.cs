﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable ConsiderUsingAsyncSuffix

namespace vtortola.WebSockets.Tools
{
    internal static class TaskHelper
    {
        public static readonly Task CanceledTask;
        public static readonly Task CompletedTask;
        public static readonly string DefaultAggregateExceptionMessage;
        public static readonly Task ExpiredTask;
        public static readonly Task<bool> TrueTask;
        public static readonly Task<bool> FalseTask;

        static TaskHelper()
        {
            CompletedTask = Task.FromResult<object>(null);
            TrueTask = Task.FromResult<bool>(true);
            FalseTask = Task.FromResult<bool>(false);

            var expired = new TaskCompletionSource<object>();
            expired.SetException(new TimeoutException());
            ExpiredTask = expired.Task;

            var canceled = new TaskCompletionSource<object>();
            canceled.SetCanceled();
            CanceledTask = canceled.Task;

            DefaultAggregateExceptionMessage = new AggregateException().Message;
        }

        public static Task FailedTask(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error), "error != null");

            return FailedTask<object>(error);
        }
        public static Task<T> FailedTask<T>(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error), "error != null");

            error = error.Unwrap();

            var tc = new TaskCompletionSource<T>();
            if (error is OperationCanceledException) tc.SetCanceled();
            else if (error is AggregateException && string.Equals(error.Message, DefaultAggregateExceptionMessage, StringComparison.Ordinal)) tc.SetException((error as AggregateException).InnerExceptions);
            else tc.SetException(error);
            return tc.Task;
        }

        public static Task FailedTask(IEnumerable<Exception> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors), "errors != null");

            return FailedTask<object>(errors);
        }
        public static Task<T> FailedTask<T>(IEnumerable<Exception> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors), "errors != null");

            var tc = new TaskCompletionSource<T>();
            tc.SetException(errors);
            return tc.Task;
        }

        public static Task IgnoreFault(
            this Task task,
            CancellationToken cancellationToken = default(CancellationToken),
            TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

            if (scheduler == null) scheduler = TaskScheduler.Current ?? TaskScheduler.Default;

            if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled) return task;

            return task.ContinueWith(t =>
            {
                // ReSharper disable once UnusedVariable
                var error = t.Exception;
#if DEBUG
                if (error != null)
                {
                    Debug.WriteLine("Ignored exception in task:");
                    Debug.Indent();
                    Debug.WriteLine(error);
                    Debug.Unindent();
                }
#endif

                if (t.IsCanceled) throw new TaskCanceledException();
            }, cancellationToken, options, scheduler);
        }
        public static Task<T> IgnoreFault<T>(
            this Task<T> task,
            T defaultResult = default(T),
            CancellationToken cancellationToken = default(CancellationToken),
            TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

            if (scheduler == null) scheduler = TaskScheduler.Current ?? TaskScheduler.Default;

            return task.ContinueWith((t, s) =>
            {
                // ReSharper disable once UnusedVariable
                var error = t.Exception;
#if DEBUG
                if (error != null)
                {
                    Debug.WriteLine("Ignored exception in task:");
                    Debug.Indent();
                    Debug.WriteLine(error);
                    Debug.Unindent();
                }
#endif

                if (t.IsCanceled) throw new TaskCanceledException();

                if (t.Status == TaskStatus.RanToCompletion) return t.Result;

                return (T)s;
            }, defaultResult, cancellationToken, options, scheduler);
        }
        public static Task IgnoreFaultOrCancellation(
            this Task task,
            CancellationToken cancellationToken = default(CancellationToken),
            TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

            if (scheduler == null) scheduler = TaskScheduler.Current ?? TaskScheduler.Default;

            if (task.IsCompleted) return CompletedTask;

            return task.ContinueWith(t =>
            {
                // ReSharper disable once UnusedVariable
                var error = t.Exception;
#if DEBUG
                if (error != null)
                {
                    Debug.WriteLine("Ignored exception in task:");
                    Debug.Indent();
                    Debug.WriteLine(error);
                    Debug.Unindent();
                }
#endif
            }, cancellationToken, options, scheduler);
        }
        public static Task<T> IgnoreFaultOrCancellation<T>(
            this Task<T> task,
            T defaultResult = default(T),
            CancellationToken cancellationToken = default(CancellationToken),
            TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

            if (scheduler == null) scheduler = TaskScheduler.Current ?? TaskScheduler.Default;

            if (task.IsCompleted) return Task.FromResult(task.Status == TaskStatus.RanToCompletion ? task.Result : defaultResult);

            return task.ContinueWith((t, s) =>
            {
                // ReSharper disable once UnusedVariable
                var error = t.Exception;
#if DEBUG
                if (error != null)
                {
                    Debug.WriteLine("Ignored exception in task:");
                    Debug.Indent();
                    Debug.WriteLine(error);
                    Debug.Unindent();
                }
#endif

                if (t.Status == TaskStatus.RanToCompletion) return t.Result;

                return (T)s;
            }, defaultResult, cancellationToken, options, scheduler);
        }

        public static void LogFault(this Task task, ILogger log, string message = null, [CallerMemberName] string memberName = "Task", [CallerFilePath] string sourceFilePath = "<no file>", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (task == null) throw new ArgumentNullException(nameof(task), "task != null");
            if (log == null) throw new ArgumentNullException(nameof(log), "log != null");

            var sourceFileName = sourceFilePath != null ? Path.GetFileName(sourceFilePath) : "<no file>";
            task.ContinueWith(faultedTask => log.Error($"[{sourceFileName}:{sourceLineNumber.ToString()}, {memberName}] {message ?? "An error occurred while performing task"}.", faultedTask.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
        }
    }
}

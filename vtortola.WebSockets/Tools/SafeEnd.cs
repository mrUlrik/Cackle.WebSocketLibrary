﻿using JetBrains.Annotations;

namespace vtortola.WebSockets
{
    public static class SafeEnd
    {
        public static void Dispose<T>([CanBeNull] T disposable, ILogger log = null) where T : class, IDisposable
        {
            if (log == null)
            {
#if DEBUG
                log = DebugLogger.Instance;
#else
                log = NullLogger.Instance;
#endif
            }

            try
            {
                disposable?.Dispose();
            }
            catch (Exception disposeError)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"{typeof(T)} dispose cause error.", disposeError);
            }
        }

        public static void ReleaseSemaphore(SemaphoreSlim semaphore, ILogger log = null)
        {
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // Held threads may try to release an already
                // disposed semaphore
            }
            catch (Exception releaseError)
            {
                if (log?.IsDebugEnabled ?? false)
                    log.Debug("Semaphore release cause error.", releaseError);
            }
        }
    }
}
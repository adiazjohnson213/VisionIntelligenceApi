namespace VisionIntelligenceAPI.Resilience
{
    public sealed class ConcurrencyLimiter
    {
        private readonly SemaphoreSlim _semaphore;

        public int MaxConcurrency { get; }

        public ConcurrencyLimiter(int maxConcurrency)
        {
            if (maxConcurrency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            }

            MaxConcurrency = maxConcurrency;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await action(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await action(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

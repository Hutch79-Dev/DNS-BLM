using Microsoft.Extensions.Logging;

namespace DNS_BLM.Infrastructure.Services
{
    public class RetryService(ILogger<RetryService> logger)
    {
        /// <summary>
        /// Executes a function with retry logic and exponential backoff.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <param name="maxRetrys">The maximum number of attempts to make. Must be 1 or higher. Defaults to 3</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The result of the function if successful, or the result of the last attempt if all retries fail.</returns>
        /// <remarks>
        /// This method retries the provided function up to <paramref name="maxRetrys"/> times.
        /// It swallows exceptions on intermediate attempts and applies an exponential backoff delay before retrying.
        /// </remarks>
        public async Task<TResult?> Retry<TResult>(Func<Task<RetryResult<TResult>?>> func, int maxRetrys = 3, CancellationToken cancellationToken = default)
        {
            if (maxRetrys <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetrys));

            RetryResult<TResult>? result = new() { };
            for (int attempt = 0; attempt <= maxRetrys; attempt++)
            {
                try
                {
                    result = await func();
                    if (result is not null)
                    {
                        if (result.IsSuccess)
                            return result.Result;
                    }
                }
                catch when (attempt < maxRetrys)
                {
                    // Swallow exception and retry
                }

                if (attempt < maxRetrys)
                {
                    var delay = CalculateBackoffTimeSeconds(attempt);
                    logger.LogDebug("Retry not successful - Delay for {Delay} seconds", delay);
                    await Task.Delay(delay * 1000, cancellationToken);
                }
            }
            if (result is not null)
                return result.Result;
            return default;
        }

        /// <summary>
        /// Returns the delay for a given retry in seconds
        /// </summary>
        /// <param name="numberOfAttempts"></param>
        /// <returns></returns>
        private int CalculateBackoffTimeSeconds(int numberOfAttempts)
        {
            numberOfAttempts += 2; // Increase attempt to skip small delays
            int totalSeconds = 0;

            for (int attempt = 1; attempt <= numberOfAttempts; attempt++)
            {
                // Each attempt adds attempt² seconds of delay
                totalSeconds += attempt * attempt;
            }

            return Math.Min(totalSeconds, 30);
        }
    }

    public class RetryResult<T>()
    {
        public T Result { get; set; }
        public bool IsSuccess { get; set; }
    }
}
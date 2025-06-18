namespace DNS_BLM.Infrastructure.Services
{
    public class RetryService
    {
        /// <summary>
        /// Executes a function with retry logic and exponential backoff.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <param name="maxAttempts">The maximum number of attempts to make.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The result of the function if successful, or the result of the last attempt if all retries fail.</returns>
        /// <remarks>
        /// This method retries the provided function up to <paramref name="maxAttempts"/> times.
        /// It swallows exceptions on intermediate attempts and applies an exponential backoff delay before retrying.
        /// </remarks>
        public async Task<TResult> Retry<TResult>(Func<Task<RetryResult<TResult>>> func, int maxAttempts = 3, CancellationToken cancellationToken = default)
        {
            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            
            RetryResult<TResult> result = new RetryResult<TResult>() { };
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    result = await func();
                    if (result.IsSuccess)
                        return result.Result;
                }
                catch when (attempt < maxAttempts)
                {
                    // Swallow exception and retry
                }
            
                if (attempt < maxAttempts)
                {
                    var delay = CalculateBackoffTime(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        
            return result.Result;
        }
    
        /// <summary>
        /// Returns the delay for a given retry in seconds
        /// </summary>
        /// <param name="numberOfAttempts"></param>
        /// <returns></returns>
        private int CalculateBackoffTime(int numberOfAttempts)
        {
            numberOfAttempts += 3; // Increase attempts to skip 1 and 5 second delays
            int totalSeconds = 0;

            for (int attempt = 1; attempt <= numberOfAttempts; attempt++)
            {
                // Each attempt adds attempt² seconds of delay
                totalSeconds += attempt * attempt;
            }

            return totalSeconds;
        }
    }

    public class RetryResult<T>()
    {
        public T Result {get;set;}
        public bool IsSuccess {get;set;}
    }
}
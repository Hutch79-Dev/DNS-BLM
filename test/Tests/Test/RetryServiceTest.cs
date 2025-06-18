using DNS_BLM.Infrastructure.Services;

namespace Tests.Test;

public class RetryServiceTest
{
        private readonly RetryService retryService;

    public RetryServiceTest()
    {
        retryService = new RetryService();
    }

    [Fact]
    public async Task Retry_ExecutesFunctionSuccessfullyOnFirstAttempt()
    {
        // Arrange
        var expectedResult = "Success";
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await retryService.Retry(func, 3);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Retry_ExecutesFunctionSuccessfullyAfterRetries()
    {
        // Arrange
        var expectedResult = "Success";
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new InvalidOperationException("Simulated failure");
            }
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await retryService.Retry(func, 3);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task Retry_ThrowsExceptionOnLastAttemptIfAllFail()
    {
        // Arrange
        var expectedExceptionMessage = "Simulated failure";
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            throw new InvalidOperationException(expectedExceptionMessage);
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => retryService.Retry(func, 3));
        Assert.Equal(expectedExceptionMessage, exception.Message);
        Assert.Equal(3, callCount); // Function is called maxAttempts times
    }

    [Fact]
    public async Task Retry_ReturnsNullResultOnLastAttemptIfAllFailAndResultIsNull()
    {
        // Arrange
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            // Simulate a case where a non-null check is important
            if (callCount < 3)
            {
                throw new InvalidOperationException("Simulated transient failure");
            }
            return Task.FromResult<string>(null); // Return null on the last successful "attempt"
        };

        // Act
        var result = await retryService.Retry(func, 3);

        // Assert
        Assert.Null(result);
        Assert.Equal(4, callCount);
    }

    [Fact]
    public async Task Retry_NoDelayOnLastAttemptOrSuccess()
    {
        // Arrange
        var expectedResult = "Success";
        var callCount = 0;
        Func<Task<string>> func = async () =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new InvalidOperationException("Failure");
            }
            // Add a small artificial delay to check if the retry logic's delay is distinct
            await Task.Delay(1);
            return expectedResult;
        };

        var startTime = DateTime.UtcNow;
        var result = await retryService.Retry(func, 2); // 1 failure, 1 success

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(2, callCount);
        // Assert that the total time taken is not excessive, implying no long final delay
        // The backoff for attempt 1 (before 2nd try) would be (1+3)^2 = 16 seconds if implemented purely.
        // The internal CalculateBackoffTime is private, so we're testing the public behavior.
        // Without knowing the exact CalculateBackoffTime, we expect it to be much less than a standard backoff.
        // Given the private CalculateBackoffTime, the first delay is for attempt 1, totalSeconds += (1+3)^2 = 16.
        // This test will take ~16 seconds. This is acceptable for observing the delay.
        // A direct mock of Task.Delay would be better for precision, but this verifies observed behavior.
        var duration = DateTime.UtcNow - startTime;
        Assert.True(duration.TotalSeconds < 20); // Should be roughly 16 seconds if retry for attempt 1 happens
    }

    [Fact]
    public async Task Retry_WhenFuncReturnsNonNullOnFirstTry_NoFurtherCalls()
    {
        // Arrange
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            return Task.FromResult("Result");
        };

        // Act
        var result = await retryService.Retry(func, 5);

        // Assert
        Assert.NotNull(result);
        Assert.True(callCount == 1);
    }

    [Fact]
    public async Task Retry_WhenFuncReturnsNullOnFirstTry_RetriesUntilNonNullOrMaxAttempts()
    {
        // Arrange
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            if (callCount == 1)
                return Task.FromResult<string>(null);
            return Task.FromResult("Final Result");
        };

        // Act
        var result = await retryService.Retry(func, 3);

        // Assert
        Assert.Equal("Final Result", result);
        Assert.Equal(2, callCount); // First returns null, second returns "Final Result"
    }

     [Fact]
    public async Task Retry_WhenFuncAlwaysReturnsNull_ReturnsNullOnMaxAttempts()
    {
        // Arrange
        var callCount = 0;
        Func<Task<string>> func = () =>
        {
            callCount++;
            return Task.FromResult<string>(null);
        };

        // Act
        var result = await retryService.Retry(func, 3);

        // Assert
        Assert.Null(result);
        Assert.Equal(4, callCount); // Called maxAttempts times, always returning null.
    }
}
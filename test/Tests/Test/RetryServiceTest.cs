using DNS_BLM.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Test
{
    public class RetryServiceTest
    {
        private readonly RetryService _retryService;

        public RetryServiceTest()
        {
            var loggerMock = new Mock<ILogger<RetryService>>();
            _retryService = new RetryService(loggerMock.Object);
        }

        [Fact]
        public async Task Retry_ExecutesFunctionSuccessfullyOnFirstAttempt()
        {
            // Arrange
            var expectedResult = "Success";
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = expectedResult, IsSuccess = true });
            };

            // Act
            var result = await _retryService.Retry(func, 1);

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
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // Simulate an unsuccessful attempt (not throwing, but IsSuccess = false)
                    return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = null, IsSuccess = false });
                }
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = expectedResult, IsSuccess = true });
            };

            // Act
            var result = await _retryService.Retry(func, 2);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task Retry_ThrowsExceptionOnLastAttemptIfAllFailViaException()
        {
            // Arrange
            var expectedExceptionMessage = "Simulated failure";
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                throw new InvalidOperationException(expectedExceptionMessage);
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _retryService.Retry(func, 1));
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task Retry_ReturnsDefaultOnLastAttemptIfAllFailViaIsSuccessFalse()
        {
            // Arrange
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = null, IsSuccess = false }); // Always return unsuccessful
            };

            // Act
            var result = await _retryService.Retry(func, 1);

            // Assert
            Assert.Null(result); // Default for string is null
            Assert.Equal(2, callCount); // Called maxAttempts times based on the retry logic
        }

        [Fact]
        public async Task Retry_NoDelayOnLastAttemptOrSuccess()
        {
            // Arrange
            var expectedResult = "Success";
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = async () =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Simulate an unsuccessful attempt
                    return new RetryResult<string> { Result = null, IsSuccess = false };
                }
                await Task.Delay(7); // small 7 ms delay to simulate executed code
                return new RetryResult<string> { Result = expectedResult, IsSuccess = true };
            };

            var startTime = DateTime.UtcNow;
            var result = await _retryService.Retry(func , 1); // 1 unsuccessful, 1 successful attempt

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(2, callCount);
            
            // Assert that the total time taken is not excessive, implying no redundant delay after success.
            var duration = DateTime.UtcNow - startTime;
            int delayForOneFailedAttempt = 5; // 5 Seconds represent the first delay after failure to succeed.
            Assert.True(duration.TotalSeconds < 1 + delayForOneFailedAttempt); // Allow max 1 second delay on success
        }

        [Fact]
        public async Task Retry_WhenFuncReturnsIsSuccessTrueOnFirstTry_NoFurtherCalls()
        {
            // Arrange
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = "Result", IsSuccess = true });
            };
            // Act
            var result = await _retryService.Retry(func, 1);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Result", result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task Retry_WhenFuncReturnsIsSuccessFalseOnFirstTry_RetriesUntilIsSuccessTrueOrMaxAttempts()
        {
            // Arrange
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = null, IsSuccess = false });
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = "Final Result", IsSuccess = true });
            };

            // Act
            var result = await _retryService.Retry(func, 1);

            // Assert
            Assert.Equal("Final Result", result);
            Assert.Equal(2, callCount); // First returns IsSuccess=false, second returns IsSuccess=true
        }

        [Fact]
        public async Task Retry_WhenFuncAlwaysReturnsIsSuccessFalse_ReturnsDefaultOnMaxAttempts()
        {
            // Arrange
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = null, IsSuccess = false });
            };

            // Act
            var result = await _retryService.Retry(func, 1);

            // Assert
            Assert.Null(result); // Default for string
            Assert.Equal(2, callCount); // Called maxAttempts times, always returning IsSuccess=false.
        }

        [Fact]
        public async Task Retry_WhenFuncReturnsNullRetryResult_RetriesUntilNonNullReturnOrMaxAttempts()
        {
            // Arrange
            var callCount = 0;
            Func<Task<RetryResult<string>?>> func = () =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // Simulate service returning null RetryResult (e.g., connection lost)
                    return Task.FromResult<RetryResult<string>?>(null);
                }
                return Task.FromResult<RetryResult<string>?>(new RetryResult<string> { Result = "Actual Result", IsSuccess = true });
            };

            // Act
            var result = await _retryService.Retry(func, 2);

            // Assert
            Assert.Equal("Actual Result", result);
            Assert.Equal(3, callCount);
        }
    }
}
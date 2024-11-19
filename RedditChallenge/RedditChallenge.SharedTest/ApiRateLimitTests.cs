using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditChallenge.Shared.Services;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace RedditChallenge.SharedTest
{
    [TestClass]
    public class ApiRateLimiterTests
    {
        private ApiRateLimiter _rateLimiter = null!;
        private Mock<ILogger<ApiRateLimiter>> _mockLogger = null!;

        [TestInitialize]
        public void SetUp()
        {
            // Initialize the mock logger
            _mockLogger = new Mock<ILogger<ApiRateLimiter>>();

            // Inject the mock logger into the ApiRateLimiter
            _rateLimiter = new ApiRateLimiter(_mockLogger.Object);
        }

        [TestMethod]
        public void UpdateRateLimits_ShouldUpdateValuesCorrectly()
        {
            // Arrange
            int expectedUsedRequests = 19;
            int expectedRemainingRequests = 50;
            int expectedResetTime = 30;
            TimeSpan duration = TimeSpan.FromSeconds(1);

            // Act
            _rateLimiter.UpdateRateLimits(expectedUsedRequests, expectedRemainingRequests, expectedResetTime, duration);
            var current = _rateLimiter.Values();

            // Assert
            Assert.AreEqual(expectedUsedRequests, current.UsedRequests, "Used requests should match the updated value.");
            Assert.AreEqual(expectedRemainingRequests, current.RemaininRequests, "Remaining requests should match the updated value.");
            Assert.AreEqual(expectedResetTime, current.ResetTimeSeconds, "Reset time should match the updated value.");
            Assert.AreEqual(duration, current.Duration, "Duration should match the updated value.");
        }

        [DataTestMethod]
        [DataRow(0, 99, 59, 1)]
        [DataRow(90, 10, 20, 2)]
        [DataRow(95, 5, 15, 3)]
        public void CalculateEvenDelay_ShouldReturnCorrectDelay_WhenRequestsRemain(int usedRequests, int remainingRequests, int resetTimeSeconds, int durationSeconds)
        {
            // Arrange
            TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
            _rateLimiter.UpdateRateLimits(usedRequests, remainingRequests, resetTimeSeconds, duration);

            // Act
            TimeSpan delay = _rateLimiter.CalculateEvenDelay();
            int adjustedResetTime = Math.Max(resetTimeSeconds - durationSeconds, 0);
            var expectedDelay = adjustedResetTime > 0 && remainingRequests > 0 ? (int)Math.Floor(((float)adjustedResetTime / (float)remainingRequests) * 1000) : 0;

            Console.WriteLine($"Expected delay: {expectedDelay}");
            Console.WriteLine($"Delay: {delay.TotalMilliseconds}");

            // Assert
            Assert.AreEqual(expectedDelay, delay.TotalMilliseconds,
                $"Delay should evenly distribute requests ({adjustedResetTime} seconds / {remainingRequests} requests = {(adjustedResetTime / remainingRequests) * 1000} ms).");
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldReturnFullResetDelay_WhenNoRequestsRemain()
        {
            // Arrange
            TimeSpan duration = TimeSpan.FromSeconds(5);
            _rateLimiter.UpdateRateLimits(0, 0, 30, duration);

            // Act
            TimeSpan delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(30000, delay.TotalMilliseconds, "Delay should wait for the full reset period (30 seconds = 30000ms).");
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldReturnZero_WhenResetPeriodHasPassed()
        {
            // Arrange
            TimeSpan duration = TimeSpan.FromSeconds(10);
            _rateLimiter.UpdateRateLimits(50, 5, 0, duration); // 5 requests but reset period has already passed

            // Act
            TimeSpan delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(0, delay.TotalMilliseconds, "Delay should be 0 when the reset period has passed.");
        }

        [TestMethod]
        public async Task ApplyEvenDelayAsync_ShouldWaitCorrectly()
        {
            // Arrange
            int usedRequests = 5;
            int remainingRequests = 5;
            int resetTimeSeconds = 10;
            TimeSpan duration = TimeSpan.FromSeconds(1);
            _rateLimiter.UpdateRateLimits(usedRequests, remainingRequests, resetTimeSeconds, duration);
            CancellationTokenSource cts = new CancellationTokenSource();

            // Act
            DateTime start = DateTime.Now;
            await _rateLimiter.ApplyEvenDelayAsync(cts.Token);
            DateTime end = DateTime.Now;

            // Assert
            Assert.IsTrue((end - start).TotalMilliseconds >= 1800, "Delay should wait at least 1.8 seconds (10s reset - 1s duration / 5 requests).");
        }

        [TestMethod]
        public async Task ApplyEvenDelayAsync_ShouldRespectCancellation()
        {
            // Arrange
            int usedRequests = 5;
            int remainingRequests = 5;
            int resetTimeSeconds = 10;
            TimeSpan duration = TimeSpan.FromSeconds(1);
            _rateLimiter.UpdateRateLimits(usedRequests, remainingRequests, resetTimeSeconds, duration);
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            // Act & Assert
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                async () => await _rateLimiter.ApplyEvenDelayAsync(cts.Token),
                "ApplyEvenDelayAsync should respect cancellation and throw TaskCanceledException."
            );
        }

        [TestMethod]
        public void Values_ShouldReturnCorrectState()
        {
            // Arrange
            int usedRequests = 10;
            int remainingRequests = 90;
            int resetTimeSeconds = 60;
            TimeSpan duration = TimeSpan.FromSeconds(2);

            _rateLimiter.UpdateRateLimits(usedRequests, remainingRequests, resetTimeSeconds, duration);

            // Act
            var values = _rateLimiter.Values();

            // Assert
            Assert.AreEqual(usedRequests, values.UsedRequests);
            Assert.AreEqual(remainingRequests, values.RemaininRequests);
            Assert.AreEqual(resetTimeSeconds, values.ResetTimeSeconds);
            Assert.AreEqual(duration, values.Duration, "Duration should match the updated value.");
            Assert.IsTrue((DateTime.UtcNow - values.LastUpdated).TotalSeconds < 1, "LastUpdated should be very recent.");
        }
    }
}

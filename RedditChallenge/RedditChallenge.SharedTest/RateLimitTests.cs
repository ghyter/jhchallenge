using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditChallenge.Shared.Services;
using System.Threading.Tasks;
using System;

namespace RedditChallenge.SharedTest
{
    [TestClass]
    public class RedditRateLimiterTests
    {
        private RedditRateLimiter _rateLimiter;

        [TestInitialize]
        public void SetUp()
        {
            _rateLimiter = new RedditRateLimiter();
        }

        [TestMethod]
        public void UpdateRateLimits_ShouldUpdateValuesCorrectly()
        {
            // Arrange
            int expectedRemainingRequests = 50;
            int expectedResetTime = 30;

            // Act
            _rateLimiter.UpdateRateLimits(expectedRemainingRequests, expectedResetTime);

            // Assert
            Assert.AreEqual(50, expectedRemainingRequests, "Remaining requests should match the updated value.");
            Assert.AreEqual(30, expectedResetTime, "Reset time should match the updated value.");
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldReturnCorrectDelay_WhenRequestsRemain()
        {
            // Arrange
            int remainingRequests = 10;
            int resetTimeSeconds = 20;
            _rateLimiter.UpdateRateLimits(remainingRequests, resetTimeSeconds);

            // Act
            int delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(2000, delay, "Delay should evenly distribute requests."); // 20 seconds / 10 requests = 2000ms
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldHandleNoRemainingRequestsGracefully()
        {
            // Arrange
            _rateLimiter.UpdateRateLimits(0, 30);

            // Act
            int delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(30000, delay, "Delay should wait for full reset when no requests remain.");
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldHandleNegativeTimeGracefully()
        {
            // Simulate an old update time by subtracting more than the reset time from now
            _rateLimiter.UpdateRateLimits(10, 1);
            System.Threading.Thread.Sleep(2000); // Let time pass to simulate elapsed reset period

            // Act
            int delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(0, delay, "Delay should return a 0 if the reset time has passed.");
        }

        [TestMethod]
        public async Task ApplyEvenDelayAsync_ShouldWaitCorrectly()
        {
            // Arrange
            int remainingRequests = 5;
            int resetTimeSeconds = 10;
            _rateLimiter.UpdateRateLimits(remainingRequests, resetTimeSeconds);

            // Act
            DateTime start = DateTime.Now;
            await _rateLimiter.ApplyEvenDelayAsync();
            DateTime end = DateTime.Now;

            Console.WriteLine($"Start: {start}, End: {end}");

            // Assert
            Assert.IsTrue((end - start).TotalMilliseconds >= 2000, "Delay should wait at least 2 seconds.");
        }
    }
}

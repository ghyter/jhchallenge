using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditChallenge.Shared.Services;
using System.Threading.Tasks;
using System;

namespace RedditChallenge.SharedTest
{
    [TestClass]
    public class ApiRateLimiterTests
    {
        private ApiRateLimiter _rateLimiter = new ApiRateLimiter();

        [TestInitialize]
        public void SetUp()
        {
            _rateLimiter = new ApiRateLimiter();
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

        [DataTestMethod]
        [DataRow(99, 59)]
        [DataRow(10, 20)]
        [DataRow(5, 15)]
        public void CalculateEvenDelay_ShouldReturnCorrectDelay_WhenRequestsRemain(int remainingRequests, int resetTimeSeconds)
        {
            // Arrange
            _rateLimiter.UpdateRateLimits(remainingRequests, resetTimeSeconds);

            // Act
            float delay = _rateLimiter.CalculateEvenDelay();


            var expectedDelay = (int)Math.Floor(((float)resetTimeSeconds / (float)remainingRequests) * 1000) ;
            Console.WriteLine($"Expected delay: {expectedDelay}");
            Console.WriteLine($"Delay: {delay}");

            // Assert
            Assert.AreEqual(expectedDelay, delay, 
                $"Delay should evenly distribute requests ({resetTimeSeconds} seconds / {remainingRequests} requests = {(resetTimeSeconds / remainingRequests) * 1000} ms).");
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldReturnFullResetDelay_WhenNoRequestsRemain()
        {
            // Arrange
            _rateLimiter.UpdateRateLimits(0, 30);

            // Act
            int delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(30000, delay, "Delay should wait for the full reset period (30 seconds = 30000ms).");
        }

        [TestMethod]
        public void CalculateEvenDelay_ShouldReturnZero_WhenResetPeriodHasPassed()
        {
            // Arrange
            _rateLimiter.UpdateRateLimits(5, 0); // 5 requests but reset period has already passed

            // Act
            int delay = _rateLimiter.CalculateEvenDelay();

            // Assert
            Assert.AreEqual(0, delay, "Delay should be 0 when the reset period has passed.");
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

            // Assert
            Assert.IsTrue((end - start).TotalMilliseconds >= 2000, "Delay should wait at least 2 seconds.");
        }
    }
}

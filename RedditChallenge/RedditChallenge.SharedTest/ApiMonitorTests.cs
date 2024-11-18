using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RedditChallenge.Shared.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedditChallenge.SharedTest
{
    [TestClass]
    public class ApiMonitorTests
    {
        private Mock<ILogger<ApiMonitor>> _mockLogger = null!;
        private Mock<IApiRateLimiter> _mockRateLimiter = null!;
        private ApiMonitorDelegate _mockActionDelegate = null!;
        private ApiMonitor _apiMonitor = null!;
        private Mock<IServiceProvider> _mockServiceProvider = null!;

        [TestInitialize]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<ApiMonitor>>();
            _mockRateLimiter = new Mock<IApiRateLimiter>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            // Mock action delegate that accepts IServiceProvider
            _mockActionDelegate = MockActionDelegate;

            _apiMonitor = new ApiMonitor(
                _mockLogger.Object,
                _mockRateLimiter.Object,
                _mockServiceProvider.Object
            );
        }

        private async Task<(int remainingRequests, int resetTimeSeconds)> MockActionDelegate(IServiceProvider serviceProvider)
        {
            await Task.Delay(10); // Simulate async work
            return (10, 60); // Example rate limit values
        }

        [TestMethod]
        public async Task StartAsync_ShouldStartLoopWithoutErrors()
        {
            // Arrange
            var loopStartedTriggered = new ManualResetEventSlim(false);
            _apiMonitor.LoopStarted += (sender, args) => loopStartedTriggered.Set();

            // Act
            await _apiMonitor.StartAsync(_mockActionDelegate);
            loopStartedTriggered.Wait(1000); // Wait for the event to be triggered
            await _apiMonitor.StopAsync();

            // Assert
            Assert.IsTrue(loopStartedTriggered.IsSet, "LoopStarted event was not triggered.");
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ApiMonitor is starting.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ApiMonitor is stopping.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [TestMethod]
        public async Task RunLoop_ShouldCallActionDelegateAndRateLimiterMethods()
        {
            // Arrange
            _mockRateLimiter.Setup(x => x.ApplyEvenDelayAsync()).Returns(Task.CompletedTask);
            var loopCalledTriggered = new ManualResetEventSlim(false);
            _apiMonitor.LoopCalled += (sender, args) => loopCalledTriggered.Set();

            // Act
            await _apiMonitor.StartAsync(_mockActionDelegate);
            loopCalledTriggered.Wait(1000); // Wait for the event to be triggered
            await _apiMonitor.StopAsync();

            // Assert
            Assert.IsTrue(loopCalledTriggered.IsSet, "LoopCalled event was not triggered.");
            _mockRateLimiter.Verify(x => x.UpdateRateLimits(10, 60), Times.AtLeastOnce);
            _mockRateLimiter.Verify(x => x.ApplyEvenDelayAsync(), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task StopAsync_ShouldCancelLoop()
        {
            // Arrange
            _mockRateLimiter.Setup(x => x.ApplyEvenDelayAsync()).Returns(Task.CompletedTask);
            var loopStoppedTriggered = new ManualResetEventSlim(false);
            _apiMonitor.LoopStopped += (sender, args) => loopStoppedTriggered.Set();

            _mockActionDelegate = async (serviceProvider) =>
            {
                await Task.Delay(10); // Simulate API call delay
                return (10, 60);
            };

            _apiMonitor = new ApiMonitor(
                _mockLogger.Object,
                _mockRateLimiter.Object,
                _mockServiceProvider.Object
            );

            // Act
            await _apiMonitor.StartAsync(_mockActionDelegate);
            await Task.Delay(50); // Allow loop to start

            // Trigger cancellation
            await _apiMonitor.StopAsync();
            loopStoppedTriggered.Wait(5000); // Wait for the event to be triggered

            // Allow time for the cancellation to process and logs to be written
            await Task.Delay(50);

            // Assert
            Assert.IsTrue(loopStoppedTriggered.IsSet, "LoopStopped event was not triggered.");
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("RunLoop exiting.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [TestMethod]
        public async Task RunningSince_ShouldTrackStartTimeCorrectly()
        {
            // Act
            var beforeStartTime = DateTime.UtcNow;
            await _apiMonitor.StartAsync(_mockActionDelegate);
            var runningSince = _apiMonitor.RunningSince;

            // Assert
            Assert.IsTrue(runningSince.HasValue);
            Assert.IsTrue(runningSince >= beforeStartTime);

            // Cleanup
            await _apiMonitor.StopAsync();
        }

        [TestMethod]
        public async Task Status_ShouldReturnCorrectState()
        {
            // Act & Assert

            // Initially, the monitor should not be running
            Assert.IsFalse(_apiMonitor.Status().IsRunning);
            Assert.IsNull(_apiMonitor.RunningSince);

            // Start the monitor
            await _apiMonitor.StartAsync(_mockActionDelegate);
            Assert.IsTrue(_apiMonitor.Status().IsRunning);
            Assert.IsNotNull(_apiMonitor.RunningSince);

            // Stop the monitor
            await _apiMonitor.StopAsync();
            Assert.IsFalse(_apiMonitor.Status().IsRunning);
            Assert.IsNull(_apiMonitor.RunningSince);
        }
    }
}

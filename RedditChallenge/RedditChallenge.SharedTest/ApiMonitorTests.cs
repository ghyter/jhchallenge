using Microsoft.Extensions.Logging;
using Moq;
using RedditChallenge.Shared.Services;

namespace RedditChallenge.SharedTest;

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
        _mockActionDelegate = MockActionDelegate;

        _apiMonitor = new ApiMonitor(
            _mockLogger.Object,
            _mockRateLimiter.Object,
            _mockServiceProvider.Object
        );
    }

    private async Task<(int calledRequests, int remainingRequests, int resetTimeSeconds)> MockActionDelegate(IServiceProvider serviceProvider)
    {
        return await Task.FromResult((50,10, 60)); // Simulate async work
    }

    [TestMethod]
    public async Task StartAsync_ShouldStartLoopWithoutErrors()
    {
        // Arrange
        var loopStartedTriggered = false;
        _apiMonitor.LoopStarted += (sender, args) => loopStartedTriggered = true;

        // Act
        await _apiMonitor.StartAsync(_mockActionDelegate);

        // Assert
        Assert.IsTrue(loopStartedTriggered, "LoopStarted event was not triggered.");
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("ApiMonitor is starting.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

 public async Task RunLoop_ShouldCallActionDelegateAndRateLimiterMethods()
{
    // Arrange
    _mockRateLimiter.Setup(x => x.ApplyEvenDelayAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

    var loopCalledTaskSource = new TaskCompletionSource<bool>();
    _apiMonitor.LoopCalled += (sender, args) => loopCalledTaskSource.TrySetResult(true);

    const int timeoutMilliseconds = 1000;

    // Act
    await _apiMonitor.StartAsync(_mockActionDelegate);

    // Wait for the event or timeout
    var completedTask = await Task.WhenAny(loopCalledTaskSource.Task, Task.Delay(timeoutMilliseconds));

    // Assert
    if (completedTask != loopCalledTaskSource.Task)
    {
        Assert.Fail("LoopCalled event was not triggered within the timeout.");
    }

    // Ensure the task completed successfully
    Assert.IsTrue(loopCalledTaskSource.Task.IsCompletedSuccessfully, "LoopCalled event did not complete successfully.");

    _mockRateLimiter.Verify(x => x.UpdateRateLimits(50, 10, 60,TimeSpan.FromMilliseconds(150)), Times.AtLeastOnce);
    _mockRateLimiter.Verify(x => x.ApplyEvenDelayAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
}


    [TestMethod]
    public async Task StopAsync_ShouldCancelLoopImmediately()
    {
        // Arrange
        _mockRateLimiter.Setup(x => x.ApplyEvenDelayAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var loopStoppedTriggered = false;
        _apiMonitor.LoopStopped += (sender, args) => loopStoppedTriggered = true;

        // Act
        await _apiMonitor.StartAsync(_mockActionDelegate);
        await _apiMonitor.StopAsync();

        await Task.Delay(1000);
        // Assert
        Assert.IsTrue(loopStoppedTriggered, "LoopStopped event was not triggered.");
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("RunLoop exiting.")),
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
        // Assert: Initially, the monitor should not be running
        Assert.IsFalse(_apiMonitor.Status().IsRunning);
        Assert.IsNull(_apiMonitor.RunningSince);

        // Act: Start the monitor
        await _apiMonitor.StartAsync(_mockActionDelegate);
        Assert.IsTrue(_apiMonitor.Status().IsRunning);
        Assert.IsNotNull(_apiMonitor.RunningSince);

        // Act: Stop the monitor
        await _apiMonitor.StopAsync();
        Assert.IsFalse(_apiMonitor.Status().IsRunning);
        Assert.IsNull(_apiMonitor.RunningSince);
    }
}

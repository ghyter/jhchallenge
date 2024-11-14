using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RedditChallenge.Shared.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedditChallenge.SharedTest;


[TestClass]
public class ApiMonitorTests
{
    private Mock<ILogger<ApiMonitor>>? _mockLogger;
    private Mock<IApiRateLimiter>? _mockRateLimiter;
    private Func<Task<(int remainingRequests, int resetTimeSeconds)>>? _mockActionDelegate;
    private ApiMonitor? _apiMonitor;

    [TestInitialize]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ApiMonitor>>();
        _mockRateLimiter = new Mock<IApiRateLimiter>();
        _mockActionDelegate = MockActionDelegate;

        _apiMonitor = new ApiMonitor(
            _mockLogger.Object,
            _mockRateLimiter.Object
        );
    }

    private async Task<(int remainingRequests, int resetTimeSeconds)> MockActionDelegate()
    {
        await Task.Delay(10); // Simulate async work
        return (10, 60); // Example rate limit values
    }

    [TestMethod]
    public async Task StartAsync_ShouldStartLoopWithoutErrors()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        await _apiMonitor.StartAsync(cancellationToken);
        await Task.Delay(100); // Give the loop time to execute
        await _apiMonitor.StopAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ApiMonitor is starting.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );

        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ApiMonitor is stopping.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task RunLoop_ShouldCallActionDelegateAndRateLimiterMethods()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockRateLimiter.Setup(x => x.ApplyEvenDelayAsync()).Returns(Task.CompletedTask);

        // Act
        await _apiMonitor.StartAsync(cancellationToken);
        await Task.Delay(100); // Allow the loop to execute
        await _apiMonitor.StopAsync(cancellationToken);

        // Assert
        _mockRateLimiter.Verify(x => x.UpdateRateLimits(10, 60), Times.AtLeastOnce);
        _mockRateLimiter.Verify(x => x.ApplyEvenDelayAsync(), Times.AtLeastOnce);
    }

  [TestMethod]
public async Task StopAsync_ShouldCancelLoop()
{
    // Arrange
    var cancellationTokenSource = new CancellationTokenSource();
    _mockRateLimiter.Setup(x => x.ApplyEvenDelayAsync()).Returns(Task.CompletedTask);

    _mockActionDelegate = async () =>
    {
        await Task.Delay(10); // Simulate API call delay
        return (10, 60);
    };

    _apiMonitor = new ApiMonitor(
        _mockLogger.Object,
        _mockRateLimiter.Object
    );

    _apiMonitor.SetActionDelegate(_mockActionDelegate);

    // Act
    await _apiMonitor.StartAsync(cancellationTokenSource.Token);
    await Task.Delay(50); // Allow loop to start

    // Trigger cancellation
    await _apiMonitor.StopAsync(cancellationTokenSource.Token);

    // Allow time for the cancellation to process and logs to be written
    await Task.Delay(50);

    // Assert
    _mockLogger.Verify(
        logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("RunLoop canceled.")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ),
        Times.Once
    );
}
}

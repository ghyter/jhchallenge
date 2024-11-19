using Microsoft.Extensions.Logging;
using Moq;
using RedditChallenge.Shared.Model;
using RedditChallenge.Shared.Services;

namespace RedditChallenge.Tests.Services;

[TestClass]
    public class RedditStatsServiceTests
    {
        private Mock<ILogger<RedditStatsService>> _mockLogger = null!;
        private RedditStatsService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<RedditStatsService>>();
            _service = new RedditStatsService(_mockLogger.Object);
        }

        [TestMethod]
        public void UpdateSubredditStats_ShouldUpdateStatsCorrectly()
        {
            // Arrange
            var response = new RedditRootResponse<SubredditResponse>
            {
                Data = new RedditResponseData<SubredditResponse>
                {
                    Children = new List<SubredditResponse>
                    {
                        new SubredditResponse { Data = new SubRedditPost { Author = "author1", Ups = 10, Title = "Post 1" } },
                        new SubredditResponse { Data = new SubRedditPost { Author = "author2", Ups = 15, Title = "Post 2" } },
                        new SubredditResponse { Data = new SubRedditPost { Author = "author1", Ups = 20, Title = "Post 3" } }
                    }
                }
            };

            // Act
            _service.UpdateSubredditStats(response);
            var stats = _service.GetLatestStats();

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreEqual("author1", stats.TopAuthorByPosts);
            Assert.AreEqual("Post 3", stats.TopUpVotedPost?.Title);
            Assert.AreEqual(20, stats.TopUpVotedPost?.Ups);
        }

        [TestMethod]
        public void UpdateSubredditStats_ShouldNotUpdate_WhenResponseIsNull()
        {
            // Arrange
            RedditRootResponse<SubredditResponse>? response = null;

            // Act
            _service.UpdateSubredditStats(response);
            var stats = _service.GetLatestStats();

            // Assert
            Assert.IsNull(stats);
            _mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(level => level == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Response data is null or missing children")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [TestMethod]
        public void UpdateSubredditStats_ShouldRaiseEvent_WhenStatsAreUpdated()
        {
            // Arrange
            var response = new RedditRootResponse<SubredditResponse>
            {
                Data = new RedditResponseData<SubredditResponse>
                {
                    Children = new List<SubredditResponse>
                    {
                        new SubredditResponse { Data = new SubRedditPost { Author = "author1", Ups = 10, Title = "Post 1" } }
                    }
                }
            };
            bool eventRaised = false;
            _service.StatsUpdated += (sender, stats) => eventRaised = true;

            // Act
            _service.UpdateSubredditStats(response);

            // Assert
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void GetLatestStats_ShouldReturnNull_Initially()
        {
            // Act
            var stats = _service.GetLatestStats();

            // Assert
            Assert.IsNull(stats);
        }
    }


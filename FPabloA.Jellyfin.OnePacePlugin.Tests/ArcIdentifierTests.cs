using FPabloA.Jellyfin.OnePacePlugin.Model;
using MediaBrowser.Controller.Providers;
using Moq;
using Xunit;

namespace FPabloA.Jellyfin.OnePacePlugin.Tests
{
    public class ArcIdentifierTests
    {
        private readonly IRepository _repository;

        private class TestArc : IArc
        {
            public int Rank { get; init; }

            public string InvariantTitle { get; init; } = null!;

            public string? MangaChapters { get; init; }

            public string Description { get; init; } = null!;

        }

        public ArcIdentifierTests()
        {
            var arcs = new List<IArc>
            {
                new TestArc
                {
                    Rank = 1,
                    InvariantTitle = "Romance Dawn",
                    MangaChapters = "1-7",
                },

                new TestArc
                {
                    Rank = 2,
                    InvariantTitle = "Orange Town",
                    MangaChapters = "8-21",
                },

                new TestArc
                {
                    Rank = 3,
                    InvariantTitle = "Syrup Village",
                    MangaChapters = null,
                },

                new TestArc
                {
                    Rank = 10,
                    InvariantTitle = "Whisky Peak",
                    MangaChapters = null,
                },

                new TestArc
                {
                    Rank = 19,
                    InvariantTitle = "Enies Lobby",
                    MangaChapters = null,
                },

                new TestArc
                {
                    Rank = 20,
                    InvariantTitle = "Post-Enies Lobby",
                    MangaChapters = null,
                }
            };

            var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
            repositoryMock
                .Setup(repository => repository.FindAllArcsAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IReadOnlyCollection<IArc>>(arcs));

            _repository = repositoryMock.Object;

        }


        [Theory]
        [InlineData("/path/to/One Pace/[One Pace][1-7] Romance Dawn [1080p]", "Romance Dawn")] // release name
        [InlineData("/path/to/One Pace/1-7", "Romance Dawn")] // chapter range
        [InlineData("/path/to/One Pace/Romance Dawn", "Romance Dawn")] // title
        [InlineData("/path/to/One Pace/1", "Romance Dawn")] // rank
        [InlineData("/path/to/One Pace/001", "Romance Dawn")] // rank (padded)
        [InlineData("/path/to/One Pace/[One Pace][8-21] Orange Town [1080p]", "Orange Town")] // release name
        [InlineData("/path/to/One Pace/[One Pace][23-41] Syrup Village [480p]", "Syrup Village")] // release name

        public async Task ShouldIdentifyArcByPath(string path, string expectedInvariantTitle)
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = path
            };

            var arc = await ArcIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.NotNull(arc);
            Assert.Equal(expectedInvariantTitle, arc.InvariantTitle);
        }

        //Regression test for titles that are substrings of other titles
        [Theory]
        [InlineData("/path/to/One Pace/Enies Lobby", "Enies Lobby")]
        [InlineData("/path/to/One Pace/Post-Enies Lobby", "Post-Enies Lobby")]
        public async Task ShouldPreferLongerTitles(string path, string expectedInvariantTitle)
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = path
            };

            var arc = await ArcIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.NotNull(arc);
            Assert.Equal(expectedInvariantTitle, arc.InvariantTitle);
        }

        //Test for common Whiskey Peak instead of Whisky Peak typo
        [Fact]
        public async Task ShouldIdentifyWhiskyPeakDespiteTypo()
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = "/path/to/One Pace/Whiskey Peak"
            };

            var arc = await ArcIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.NotNull(arc);
            Assert.Equal("Whisky Peak", arc.InvariantTitle);
        }

        //Jellyfin 10.9.x allows media to not have a path
        [Fact]
        public async Task ShouldNotCrashWithMissingPath()
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = null
            };

            var arc = await ArcIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.Null(arc);
        }

    }
}
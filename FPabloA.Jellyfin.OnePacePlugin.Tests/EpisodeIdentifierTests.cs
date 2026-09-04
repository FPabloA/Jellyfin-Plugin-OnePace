using FPabloA.Jellyfin.OnePacePlugin.Model;
using MediaBrowser.Controller.Providers;
using Moq;
using Xunit;

namespace FPabloA.Jellyfin.OnePacePlugin.Tests
{
    public class EpisodeIdentifierTests
    {
        private readonly IRepository _repository;

        private class TestEpisode : IEpisode
        {
            public int Rank { get; init; }

            public string ArcNum { get; init; }

            public string InvariantTitle { get; init; } = null!;

            public string FileTitle { get; init; } = null;

            public string? MangaChapters { get; init; }

            public DateTime? ReleaseDate { get; init; }

            public uint? Crc32 { get; init; }

            public string Description { get; init; } = null!;
        }

        public EpisodeIdentifierTests()
        {
            var episodes = new List<IEpisode>
            {
                new TestEpisode
                {
                    ArcNum = "1",
                    Rank = 1,
                    InvariantTitle = "Romance Dawn, the Dawn of an Adventure",
                    FileTitle = "Romance Dawn 01",
                    MangaChapters = "1",
                    ReleaseDate = null,
                    Crc32 = 0xD767799C
                },

                new TestEpisode
                {
                    ArcNum = "1",
                    Rank = 2,
                    InvariantTitle = "They Call Him \"Straw Hat\" Luffy",
                    FileTitle = "Romance Dawn 02",
                    MangaChapters = "2",
                    ReleaseDate = null,
                    Crc32 = 0x04A43CEF
                },

                new TestEpisode
                {
                    ArcNum = "2",
                    Rank = 1,
                    InvariantTitle = "Enter: Nami",
                    FileTitle = "Orange Town 01",
                    MangaChapters = "8-11",
                    ReleaseDate = null,
                    Crc32 = 0xC7CA5080
                },

                new TestEpisode
                {
                    ArcNum = "2",
                    Rank = 2,
                    InvariantTitle = "Treasure",
                    FileTitle = "Orange Town 02",
                    MangaChapters = null,
                    ReleaseDate = null,
                    Crc32 = null
                },

                new TestEpisode
                {
                    ArcNum = "10",
                    Rank = 1,
                    InvariantTitle = "The Town of Welcome",
                    FileTitle = "Whisky Peak 01",
                    MangaChapters = "106-109",
                    ReleaseDate = null,
                    Crc32 = null
                },

                new TestEpisode
                {
                    Rank = 1,
                    InvariantTitle = "The Superhumans of Enies Lobby",
                    FileTitle = "Enies Lobby 01",
                    MangaChapters = null,
                    ReleaseDate = null,
                    Crc32 = null
                },

                new TestEpisode
                {
                    Rank = 1,
                    InvariantTitle = "Fist of Love",
                    FileTitle = "Post-Enies Lobby 01",
                    MangaChapters = null,
                    ReleaseDate = null,
                    Crc32 = null
                }
            };

            var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
            repositoryMock
                .Setup(repository => repository.FindAllEpisodesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IReadOnlyCollection<IEpisode>>(episodes));

            _repository = repositoryMock.Object;
        }

        [Theory]
        [InlineData("/path/to/One Pace/[One Pace][1-7] Romance Dawn [1080p]/[One Pace][1] Romance Dawn 01 [1080p][D767799C].mkv", "Romance Dawn, the Dawn of an Adventure")] // nested release name
        [InlineData("/path/to/One Pace/[One Pace][1] Romance Dawn 01 [1080p][D767799C].mkv", "Romance Dawn, the Dawn of an Adventure")] // release name
        [InlineData("/path/to/One Pace/[One Pace][2] Romance Dawn 02 [1080p][04A43CEF].mkv", "They Call Him \"Straw Hat\" Luffy")] // release name
        [InlineData("/path/to/One Pace/[One Pace][8-11] Orange Town 01 [480p][A2F5F372].mkv", "Enter: Nami")] // release name
        [InlineData("/path/to/One Pace/[One Pace][11-16] Orange Town 02 [480p][3D7957D8].mkv", "Treasure")] // release name
        [InlineData("/path/to/One Pace/1.mkv", "Romance Dawn, the Dawn of an Adventure")] // chapter range only
        [InlineData("/path/to/One Pace/8-11.mkv", "Enter: Nami")] // chapter range only
        [InlineData("/path/to/One Pace/Romance Dawn 01.mkv", "Romance Dawn, the Dawn of an Adventure")] // invariant title only
        [InlineData("/path/to/One Pace/Orange Town 01.mkv", "Enter: Nami")] // invariant title only
        [InlineData("/path/to/One Pace/D767799C.mkv", "Romance Dawn, the Dawn of an Adventure")] // uppercase CRC-32 only
        [InlineData("/path/to/One Pace/c7ca5080.mkv", "Enter: Nami")] // lowercase CRC-32 only
        public async Task ShouldIdentifyEpisodeByPath(string path, string expectedInvariantTitle)
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = path
            };

            var episode = await EpisodeIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.NotNull(episode);
            Assert.Equal(expectedInvariantTitle, episode.InvariantTitle);
        }

        //Regression test for titles that are substrings of other titles
        [Theory]
        [InlineData("/path/to/One Pace/Enies Lobby 01.mkv", "The Superhumans of Enies Lobby")]
        [InlineData("/path/to/One Pace/Post-Enies Lobby 01.mkv", "Fist of Love")]
        public async Task ShouldPreferLongerTitles(string path, string expectedInvariantTitle)
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = path
            };

            var episode = await EpisodeIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.NotNull(episode);
            Assert.Equal(expectedInvariantTitle, episode.InvariantTitle);
        }

        //Test for common Whiskey Peak instead of Whisky Peak typo
        [Fact]
        public async Task ShouldIdentifyWhiskyPeakDespiteTypo()
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = "/path/to/One Pace/Whiskey Peak 01.mkv"
            };

            var episode = await EpisodeIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.NotNull(episode);
            Assert.Equal("The Town of Welcome", episode.InvariantTitle);
        }

        //Jellyfin 10.9.x allows media to not have a path
        [Fact]
        public async Task ShouldNotCrashWithMissingPath()
        {
            var itemLookupInfo = new ItemLookupInfo
            {
                Path = null
            };

            var episode = await EpisodeIdentifier.IdentifyAsync(_repository, itemLookupInfo, CancellationToken.None);

            Assert.Null(episode);
        }

    }
}

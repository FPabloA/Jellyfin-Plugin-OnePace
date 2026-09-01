using System.Net;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace FPabloA.Jellyfin.OnePacePlugin.Tests
{
    public class WebRepositoryTests
    {
        //TODO:Rework this to make it more inline with a OnePacerr Response vs a One Pace API response
        private const string MetadataResponse = """
            {
                "data": {
                    "arcs": [
                        {
                            "arc": 1,
                            "title": "Romance Dawn",
                            "mangaChapters": "1 - 7",
                            "description": "Monkey D. Luffy sets out on an adventure to form a crew, find the legendary One Piece, and become the pirate king.",
                            "translations": [
                                {
                                    "title": "Romance Dawn en",
                                    "description": "English description for Romance Dawn",
                                    "language_code": "en"
                                }
                            ],
                            "episodes": [
                                {
                                    "id": "clksyqwxl000208jw82wh3y0g",
                                    "arc": 1,
                                    "episode": 1,
                                    "title": "Romance Dawn 01",
                                    "description": "test",
                                    "mangaChapters": "1",
                                    "released": "2020-12-02T12:00:00Z",
                                    "translations": [
                                        {
                                            "title": "Romance Dawn 01 de",
                                            "description": "Deutsche Beschreibung für Romance Dawn 01",
                                            "language_code": "de"
                                        },
                                        {
                                            "title": "Romance Dawn 01 en",
                                            "description": "English description for Romance Dawn 01",
                                            "language_code": "en"
                                        }
                                    ],
                                    "files": {
                                        "standard": {
                                            "CRC32": "11000000",
                                            "hash": "cdab4a928dbbff643bbe5531f216eb36a60c85af",
                                            "magnetURI": "magnet:?xt=urn:btih:cdab4a928dbbff643bbe5531f216eb36a60c85af&dn=%5BOne+Pace%5D%5B1-7%5D+Romance+Dawn+%5B1080p%5D&tr=http%3A%2F%2Fnyaa.tracker.wf%3A7777%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=https%3A%2F%2Ftracker1.520.jp%3A443%2Fannounce&tr=udp%3A%2F%2Fopentracker.i2p.rocks%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=http%3A%2F%2Ftracker.openbittorrent.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=http%3A%2F%2Fbt.endpot.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker1.bt.moack.co.kr%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.tiny-vps.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&tr=udp%3A%2F%2Fretracker01-msk-virt.corbina.net%3A80%2Fannounce&tr=udp%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fmovies.zsw.ca%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=https%3A%2F%2Ftracker.gbitt.info%3A443%2Fannounce&tr=https%3A%2F%2Ftr.burnabyhighstar.com%3A443%2Fannounce&tr=http%3A%2F%2Ftracker.gbitt.info%3A80%2Fannounce",
                                            "duration": 1077,
                                            "variant": "standard",
                                            "partOfBundle": true
                                        }
                                    }
                                },
                                {
                                    "id": "clksys3c2000308jwa08325o7",
                                    "arc": 1,
                                    "episode": 2,
                                    "title": "Romance Dawn 02",
                                    "description": "test",
                                    "mangaChapters": "2",
                                    "released": "2020-12-02T12:00:00Z",
                                    "translations": [
                                        {
                                            "title": "Romance Dawn 02 de",
                                            "description": "Deutsche Beschreibung für Romance Dawn 02",
                                            "language_code": "de"
                                        },
                                        {
                                            "title": "Romance Dawn 02 en",
                                            "description": "English description for Romance Dawn 02",
                                            "language_code": "en"
                                        }
                                    ],
                                    "files": {
                                        "standard": {
                                            "CRC32": "12000000",
                                            "hash": "cdab4a928dbbff643bbe5531f216eb36a60c85af",
                                            "magnetURI": "magnet:?xt=urn:btih:cdab4a928dbbff643bbe5531f216eb36a60c85af&dn=%5BOne+Pace%5D%5B1-7%5D+Romance+Dawn+%5B1080p%5D&tr=http%3A%2F%2Fnyaa.tracker.wf%3A7777%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=https%3A%2F%2Ftracker1.520.jp%3A443%2Fannounce&tr=udp%3A%2F%2Fopentracker.i2p.rocks%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=http%3A%2F%2Ftracker.openbittorrent.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=http%3A%2F%2Fbt.endpot.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker1.bt.moack.co.kr%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.tiny-vps.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&tr=udp%3A%2F%2Fretracker01-msk-virt.corbina.net%3A80%2Fannounce&tr=udp%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fmovies.zsw.ca%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=https%3A%2F%2Ftracker.gbitt.info%3A443%2Fannounce&tr=https%3A%2F%2Ftr.burnabyhighstar.com%3A443%2Fannounce&tr=http%3A%2F%2Ftracker.gbitt.info%3A80%2Fannounce",
                                            "duration": 1077,
                                            "variant": "standard",
                                            "partOfBundle": true
                                        }
                                    }
                                },
                                {
                                    "id": "clksysvim000408jw6anzden8",
                                    "arc": 1,
                                    "episode": 3,
                                    "title": "Romance Dawn 03",
                                    "description": "test",
                                    "mangaChapters": null,
                                    "released": null,
                                    "translations": [],
                                    "files": {
                                        "standard": {
                                            "CRC32": null,
                                            "hash": "cdab4a928dbbff643bbe5531f216eb36a60c85af",
                                            "magnetURI": "magnet:?xt=urn:btih:cdab4a928dbbff643bbe5531f216eb36a60c85af&dn=%5BOne+Pace%5D%5B1-7%5D+Romance+Dawn+%5B1080p%5D&tr=http%3A%2F%2Fnyaa.tracker.wf%3A7777%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=https%3A%2F%2Ftracker1.520.jp%3A443%2Fannounce&tr=udp%3A%2F%2Fopentracker.i2p.rocks%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=http%3A%2F%2Ftracker.openbittorrent.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=http%3A%2F%2Fbt.endpot.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker1.bt.moack.co.kr%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.tiny-vps.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&tr=udp%3A%2F%2Fretracker01-msk-virt.corbina.net%3A80%2Fannounce&tr=udp%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fmovies.zsw.ca%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=https%3A%2F%2Ftracker.gbitt.info%3A443%2Fannounce&tr=https%3A%2F%2Ftr.burnabyhighstar.com%3A443%2Fannounce&tr=http%3A%2F%2Ftracker.gbitt.info%3A80%2Fannounce",
                                            "duration": 1077,
                                            "variant": "standard",
                                            "partOfBundle": true
                                        }
                                    }
                                }
                            ]
                        },
                        {
                            "arc": 2,
                            "title": "Orange Town",
                            "mangaChapters": "8-21",
                            "description": "Luffy and Zoro run afoul of a flashy crew of pirates and their captain, Buggy the Clown. They are joined by a young girl named Nami who helps them navigate this predicament.",
                            "translations": [
                                {
                                    "title": "Orange Town de",
                                    "description": "Deutsche Beschreibung für Orange Town",
                                    "language_code": "de"
                                },
                                {
                                    "title": "Orange Town en",
                                    "description": "English description for Orange Town",
                                    "language_code": "en"
                                }
                            ],
                            "episodes": [
                                {
                                    "id": "clksytlbt000508jw6r9x1jb1",
                                    "arc": 2,
                                    "episode": 1,
                                    "title": "Orange Town 01",
                                    "description": "test",
                                    "mangaChapters": "8-11",
                                    "released": "2021-08-07T12:00:00Z",
                                    "translations": [
                                        {
                                            "title": "Orange Town 01 de",
                                            "description": "Deutsche Beschreibung für Orange Town 01",
                                            "language_code": "de"
                                        },
                                        {
                                            "title": "Orange Town 01 en",
                                            "description": "English description for Orange Town 01",
                                            "language_code": "en"
                                        }
                                    ],
                                    "files": {
                                        "standard": {
                                            "CRC32": "21000000",
                                            "hash": "cdab4a928dbbff643bbe5531f216eb36a60c85af",
                                            "magnetURI": "magnet:?xt=urn:btih:cdab4a928dbbff643bbe5531f216eb36a60c85af&dn=%5BOne+Pace%5D%5B1-7%5D+Romance+Dawn+%5B1080p%5D&tr=http%3A%2F%2Fnyaa.tracker.wf%3A7777%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=https%3A%2F%2Ftracker1.520.jp%3A443%2Fannounce&tr=udp%3A%2F%2Fopentracker.i2p.rocks%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=http%3A%2F%2Ftracker.openbittorrent.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=http%3A%2F%2Fbt.endpot.com%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker1.bt.moack.co.kr%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.tiny-vps.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&tr=udp%3A%2F%2Fretracker01-msk-virt.corbina.net%3A80%2Fannounce&tr=udp%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fmovies.zsw.ca%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=https%3A%2F%2Ftracker.gbitt.info%3A443%2Fannounce&tr=https%3A%2F%2Ftr.burnabyhighstar.com%3A443%2Fannounce&tr=http%3A%2F%2Ftracker.gbitt.info%3A80%2Fannounce",
                                            "duration": 1077,
                                            "variant": "standard",
                                            "partOfBundle": true
                                        }
                                    }
                                }
                            ]
                        }
                    ]
                }
            }
        """;

        private readonly WebRepository _webRepository;

        public WebRepositoryTests()
        {
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    //TODO: change uri to onepacerr
                    if (request.RequestUri != null &&
                        request.Method == HttpMethod.Post &&
                        request.RequestUri.AbsoluteUri == "https://onepacerr.com/api/v1/metadata" &&
                        request.Content != null)
                    {
                        var requestContent = request.Content.ReadAsStringAsync(cancellationToken).Result;
                        if (requestContent.Contains("series") && requestContent.Contains("arcs"))
                        {
                            return Task.FromResult(new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(MetadataResponse)
                            });
                        }
                    }

                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.NotFound
                    });
                });

            var httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(httpMessageHandlerMock.Object));

            _webRepository = new WebRepository(httpClientFactoryMock.Object, new MemoryCache(new MemoryCacheOptions()),
                NullLogger<WebRepository>.Instance);
        }

        [Fact]
        public async Task ShouldFindSeries()
        {
            var result = await _webRepository.FindSeriesAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("One Pace", result.InvariantTitle);
        }

        [Fact]
        public async Task ShouldFindAllArcs()
        {
            var result = await _webRepository.FindAllArcsAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Collection(result,
                arc => Assert.Equal("Romance Dawn", arc.InvariantTitle),
                arc => Assert.Equal("Orange Town", arc.InvariantTitle));
        }

        [Fact]
        public async Task ShouldFindAllEpisodes()
        {
            var result = await _webRepository.FindAllEpisodesAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Collection(result,
                episode => Assert.Equal("Romance Dawn 01", episode.InvariantTitle),
                episode => Assert.Equal("Romance Dawn 02", episode.InvariantTitle),
                episode => Assert.Equal("Romance Dawn 03", episode.InvariantTitle),
                episode => Assert.Equal("Orange Town 01", episode.InvariantTitle));
        }

        //[Theory]
        //[InlineData("clksypeix000008jw066ye7lo", "Romance Dawn")]
        //[InlineData("clksyq4q5000108jwgihd6jud", "Orange Town")]
        //public async Task ShouldFindArcById(string arcId, string expectedInvariantTitle)
        //{
        //    var result = await _webRepository.FindArcByIdAsync(arcId, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedInvariantTitle, result.InvariantTitle);
        //}

        //TODO: Clearing stuff to do with EpisodeID
        //[Theory]
        //[InlineData("clksyqwxl000208jw82wh3y0g", "Romance Dawn 01")]
        //[InlineData("clksys3c2000308jwa08325o7", "Romance Dawn 02")]
        //[InlineData("clksysvim000408jw6anzden8", "Romance Dawn 03")]
        //[InlineData("clksytlbt000508jw6r9x1jb1", "Orange Town 01")]
        //public async Task ShouldFindEpisodeById(string episodeId, string expectedInvariantTitle)
        //{
        //    var result = await _webRepository.FindEpisodeByIdAsync(episodeId, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedInvariantTitle, result.InvariantTitle);
        //}

        ////Regression test for ArcId not being populated correctly
        /////Not using ArcIds anyways, so maybe ok to just ignore for now
        //[Fact]
        //public async Task ShouldFindEpisodeWithMatchingArcById()
        //{
        //    var result = await _webRepository.FindEpisodeByIdAsync("clksyqwxl000208jw82wh3y0g", CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal("clksyqwxl000208jw82wh3y0g", result.Id);
        //    Assert.Equal("clksypeix000008jw066ye7lo", result.ArcId);
        //}

        //TODO: This should probably just be checking its falling back to en everytime
        //TODO 2: Localization checks in general will probably be removed since we can only fetch data in english
        //[Theory]
        //[InlineData("en", "One Pace en", "English description")]
        //[InlineData("de", "One Pace de", "Deutsche Beschreibung")]
        //[InlineData("invalid", "One Pace en", "English description")]
        //public async Task ShouldFindBestSeriesLocalization(string languageCode, string expectedTitle, string expectedDescription)
        //{
        //    var result = await _webRepository.FindBestLocalizationBySeriesAsync(languageCode, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedTitle, result.Title);
        //    Assert.Equal(expectedDescription, result.Description);
        //}

        //TODO: Same as above
        //TODO:Clearing stuff to do with arcID
        //[Theory]
        //[InlineData("clksypeix000008jw066ye7lo", "en", "Romance Dawn en", "English description for Romance Dawn")]
        //[InlineData("clksyq4q5000108jwgihd6jud", "en", "Orange Town en", "English description for Orange Town")]
        //[InlineData("clksyq4q5000108jwgihd6jud", "de", "Orange Town de", "Deutsche Beschreibung für Orange Town")]
        //[InlineData("clksyq4q5000108jwgihd6jud", "invalid", "Orange Town en", "English description for Orange Town")]
        //public async Task ShouldFindBestArcLocalization(
        //    string arcId,
        //    string languageCode,
        //    string expectedTitle,
        //    string expectedDescription)
        //{
        //    var result = await _webRepository.FindBestLocalizationByArcIdAsync(arcId, languageCode, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedTitle, result.Title);
        //    Assert.Equal(expectedDescription, result.Description);
        //}

        //TODO: Clearing stuff to do with EpisodeID
        //TODO: Same as above
        //[Theory]
        //[InlineData("clksyqwxl000208jw82wh3y0g", "de", "Romance Dawn 01 de", "Deutsche Beschreibung für Romance Dawn 01")]
        //[InlineData("clksyqwxl000208jw82wh3y0g", "en", "Romance Dawn 01 en", "English description for Romance Dawn 01")]
        //[InlineData("clksyqwxl000208jw82wh3y0g", "invalid", "Romance Dawn 01 en", "English description for Romance Dawn 01")]
        //public async Task ShouldFindBestEpisodeLocalization(
        //    string episodeId,
        //    string languageCode,
        //    string expectedTitle,
        //    string expectedDescription)
        //{
        //    var result = await _webRepository.FindBestLocalizationByEpisodeIdAsync(episodeId, languageCode, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedTitle, result.Title);
        //    Assert.Equal(expectedDescription, result.Description);
        //}

        ////TODO:Not sure what this should be checking since one pacerr will not return art, but we must override this method for jellyfin
        //[Fact]
        //public async Task ShouldFindSeriesLogoArt()
        //{
        //    var result = await _webRepository.FindAllLogoArtBySeriesAsync(CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.NotEmpty(result);
        //}

        ////TODO:same as above
        //[Fact]
        //public async Task ShouldFindSeriesCoverArt()
        //{
        //    var result = await _webRepository.FindAllCoverArtBySeriesAsync(CancellationToken.None);

        //    Assert.NotNull(result);
        //}

        ////TODO:same as above
        //[Theory]
        //[InlineData("clksypeix000008jw066ye7lo", 4)]
        //[InlineData("clksyq4q5000108jwgihd6jud", 1)]
        //public async Task ShouldFindAllArcCoverArt(string arcId, int expectedCoverArtCount)
        //{
        //    var result = await _webRepository.FindAllCoverArtByArcIdAsync(arcId, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedCoverArtCount, result.Count);
        //}

        ////TODO:same as above
        //[Theory]
        //[InlineData("clksyqwxl000208jw82wh3y0g", 3)]
        //[InlineData("clksys3c2000308jwa08325o7", 1)]
        //[InlineData("clksytlbt000508jw6r9x1jb1", 2)]
        //public async Task ShouldFindAllEpisodeCoverArt(string episodeId, int expectedCoverArtCount)
        //{
        //    var result = await _webRepository.FindAllCoverArtByEpisodeIdAsync(episodeId, CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Equal(expectedCoverArtCount, result.Count);
        //}

    }
}
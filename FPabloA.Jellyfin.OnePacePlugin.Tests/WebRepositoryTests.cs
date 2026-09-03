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
            [
            {
                    "arc": 0,
                    "title": "Specials",
                    "description": "specials arc description",
                    "episodes": [
                        {
                            "arc": 0,
                            "episode": 1,
                            "title": "One Piece Fan Letter",
                            "description": "test for special",
                            "files": {
                                "standard": {
                                    "CRC32": "00000000",
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
                    "arc": 1,
                    "title": "Romance Dawn",
                    "mangaChapters": "1 - 7",
                    "description": "romance dawn arc description",
                    "episodes": [
                        {
                            "arc": 1,
                            "episode": 1,
                            "title": "Romance Dawn, the Dawn of an Adventure",
                            "description": "test for romance dawn",
                            "mangaChapters": "1",
                            "released": "2020-12-02T12:00:00Z",
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
                            "arc": 1,
                            "episode": 2,
                            "title": "They Call Him \"Straw Hat\" Luffy",
                            "description": "test for romance dawn",
                            "mangaChapters": "2",
                            "released": "2020-12-02T12:00:00Z",
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
                            "arc": 1,
                            "episode": 3,
                            "title": "The Pirate King and the Master Swordsman",
                            "description": "test for romance dawn",
                            "mangaChapters": null,
                            "released": null,
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
                    "description": "orange town arc description",
                    "episodes": [
                        {
                            "arc": 2,
                            "episode": 1,
                            "title": "Enter: Nami",
                            "description": "test for orange town",
                            "mangaChapters": "8-11",
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
                        request.Method == HttpMethod.Get &&
                        request.RequestUri.AbsoluteUri == "https://onepacerr.com/api/v1/metadata/arcs/?episodes=true&files=true")
                    {
                        var requestContent = request.Content.ReadAsStringAsync(cancellationToken).Result;
                        //if (requestContent.Contains("series") && requestContent.Contains("arcs"))
                        //{
                            return Task.FromResult(new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(MetadataResponse)
                            });
                        //}
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
                arc => Assert.Equal("Specials", arc.InvariantTitle),
                arc => Assert.Equal("Romance Dawn", arc.InvariantTitle),
                arc => Assert.Equal("Orange Town", arc.InvariantTitle));
        }

        [Fact]
        public async Task ShouldPopulateArcDescriptionsCorrectly()
        {
            var result = await _webRepository.FindAllArcsAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Collection(result,
                arc => Assert.Equal("specials arc description", arc.Description),
                arc => Assert.Equal("romance dawn arc description", arc.Description),
                arc => Assert.Equal("orange town arc description", arc.Description)
                );
        }

        [Fact]
        public async Task ShouldFindAllEpisodes()
        {
            var result = await _webRepository.FindAllEpisodesAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Collection(result,
                episode => Assert.Equal("One Piece Fan Letter", episode.InvariantTitle),
                episode => Assert.Equal("Romance Dawn, the Dawn of an Adventure", episode.InvariantTitle),
                episode => Assert.Equal("They Call Him \"Straw Hat\" Luffy", episode.InvariantTitle),
                episode => Assert.Equal("The Pirate King and the Master Swordsman", episode.InvariantTitle),
                episode => Assert.Equal("Enter: Nami", episode.InvariantTitle));
        }

        [Fact]
        public async Task ShouldConstructFileTitlesCorrectly()
        {
            var result = await _webRepository.FindAllEpisodesAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Collection(result,
                episode => Assert.Equal("Specials 01", episode.FileTitle),
                episode => Assert.Equal("Romance Dawn 01", episode.FileTitle),
                episode => Assert.Equal("Romance Dawn 02", episode.FileTitle),
                episode => Assert.Equal("Romance Dawn 03", episode.FileTitle),
                episode => Assert.Equal("Orange Town 01", episode.FileTitle));
        }

        [Fact]
        public async Task ShouldPopulateEpisodeDescriptionsCorrectly()
        {
            var result = await _webRepository.FindAllEpisodesAsync(CancellationToken.None);
            Assert.NotNull(result);
            Assert.Collection(result,
                episode => Assert.Equal("test for special", episode.Description),
                episode => Assert.Equal("test for romance dawn", episode.Description),
                episode => Assert.Equal("test for romance dawn", episode.Description),
                episode => Assert.Equal("test for romance dawn", episode.Description),
                episode => Assert.Equal("test for orange town", episode.Description)
                );
        }

        //Tests same thing as find all arcs, but with an actual request; retrieves all arcs, only uncomment to test with the mock httpmessagehandler commented out
        //[Fact]
        //public async Task ShouldFindAllArcsActual()
        //{
        //    var result = await _webRepository.FindAllArcsAsync(CancellationToken.None);

        //    Assert.NotNull(result);
        //    Assert.Collection(result,
        //        arc => Assert.Equal("Specials", arc.InvariantTitle),
        //        arc => Assert.Equal("Romance Dawn", arc.InvariantTitle),
        //        arc => Assert.Equal("Orange Town", arc.InvariantTitle),
        //        arc => Assert.Equal("Syrup Village", arc.InvariantTitle),
        //        arc => Assert.Equal("Gaimon", arc.InvariantTitle),
        //        arc => Assert.Equal("Baratie", arc.InvariantTitle),
        //        arc => Assert.Equal("Arlong Park", arc.InvariantTitle),
        //        arc => Assert.Equal("The Adventures of Buggy's Crew", arc.InvariantTitle),
        //        arc => Assert.Equal("Loguetown", arc.InvariantTitle),
        //        arc => Assert.Equal("Reverse Mountain", arc.InvariantTitle),
        //        arc => Assert.Equal("Whisky Peak", arc.InvariantTitle),
        //        arc => Assert.Equal("The Trials of Koby-Meppo", arc.InvariantTitle),
        //        arc => Assert.Equal("Little Garden", arc.InvariantTitle),
        //        arc => Assert.Equal("Drum Island", arc.InvariantTitle),
        //        arc => Assert.Equal("Alabasta", arc.InvariantTitle),
        //        arc => Assert.Equal("Jaya", arc.InvariantTitle),
        //        arc => Assert.Equal("Skypeia", arc.InvariantTitle),
        //        arc => Assert.Equal("Long Ring Long Land", arc.InvariantTitle),
        //        arc => Assert.Equal("Water Seven", arc.InvariantTitle),
        //        arc => Assert.Equal("Enies Lobby", arc.InvariantTitle),
        //        arc => Assert.Equal("Post-Enies Lobby", arc.InvariantTitle),
        //        arc => Assert.Equal("Thriller Bark", arc.InvariantTitle),
        //        arc => Assert.Equal("Sabaody Archipelago", arc.InvariantTitle),
        //        arc => Assert.Equal("Amazon Lily", arc.InvariantTitle),
        //        arc => Assert.Equal("Impel Down", arc.InvariantTitle),
        //        arc => Assert.Equal("If You Could Go Anywhere... The Adventures of the Straw Hats", arc.InvariantTitle),
        //        arc => Assert.Equal("Marineford", arc.InvariantTitle),
        //        arc => Assert.Equal("Post-War", arc.InvariantTitle),
        //        arc => Assert.Equal("Return to Sabaody", arc.InvariantTitle),
        //        arc => Assert.Equal("Fishman Island", arc.InvariantTitle),
        //        arc => Assert.Equal("Punk Hazard", arc.InvariantTitle),
        //        arc => Assert.Equal("Dressrosa", arc.InvariantTitle),
        //        arc => Assert.Equal("Zou", arc.InvariantTitle),
        //        arc => Assert.Equal("Whole Cake Island", arc.InvariantTitle),
        //        arc => Assert.Equal("Reverie", arc.InvariantTitle),
        //        arc => Assert.Equal("Wano", arc.InvariantTitle),
        //        arc => Assert.Equal("Egghead", arc.InvariantTitle)
        //        );
        //}

    }
}
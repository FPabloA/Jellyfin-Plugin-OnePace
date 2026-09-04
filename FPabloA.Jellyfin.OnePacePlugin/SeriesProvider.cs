using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

//Populates One Pace series metadata from the OnePacerr API (not sure if onepacerr has series metadata)

namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class SeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private const string OnePaceDesc = "One Pace is a fan project that recuts the One Piece anime in an endeavor to bring it more in line with the pacing of the original manga by Eiichiro Oda. The team accomplishes this by removing filler scenes not present in the source material. This process requires meticulous editing and quality control to ensure seamless music and transitions.";
        private readonly IRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SeriesProvider> _log;

        public SeriesProvider(IRepository repository, IHttpClientFactory httpClientFactory, ILogger<SeriesProvider> logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _log = logger;
        }

        public int Order => -1000;

        public string Name => Plugin.ProviderName;

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var seriesMatch = await SeriesIdentifier.IdentifyAsync(_repository, info, cancellationToken).ConfigureAwait(false);
            if (seriesMatch != null)
            {
                result.HasMetadata = true;
                result.Provider = Name;

                result.Item = new Series
                {
                    Name = seriesMatch.InvariantTitle,
                    OriginalTitle = seriesMatch.OriginalTitle,
                    Overview = OnePaceDesc
                };

                result.Item.SetOnePaceId(Plugin.DummySeriesId);
                result.Item.SetProviderId("AniDB", "69"); // https://anidb.net/anime/69
                result.Item.SetProviderId("AniList", "21"); // https://anilist.co/anime/21/ONE-PIECE/

            }

            _log.LogInformation(
                "Identified Series {Info} --> {Match}",
                JsonSerializer.Serialize(info),
                JsonSerializer.Serialize(seriesMatch));

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            SeriesInfo searchInfo,
            CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            var metadataResult = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);
            if (metadataResult.HasMetadata)
            {
                var series = metadataResult.Item;

                result.Add(new RemoteSearchResult
                {
                    Name = series.Name,
                    Overview = series.Overview,
                    ProviderIds = series.ProviderIds,
                    SearchProviderName = Name
                });
            }

            return result;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }

    }
}

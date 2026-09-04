using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ICU4N.Util;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

//Populates One Pace episode metadata from the onepacerr API
namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class EpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly IRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EpisodeProvider> _log;

        public EpisodeProvider(
            IRepository repository,
            IHttpClientFactory httpClientFactory,
            ILogger<EpisodeProvider> logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _log = logger;
        }

        public int Order => -1000;

        public string Name => Plugin.ProviderName;

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>();

            var episodeMatch = await EpisodeIdentifier.IdentifyAsync(_repository, info, cancellationToken).ConfigureAwait(false);
            if (episodeMatch != null)
            {

                var arc = await _repository.FindArcByNumberAsync(episodeMatch.ArcNum, cancellationToken).ConfigureAwait(false); ;
                if (arc != null)
                {
                    result.HasMetadata = true;
                    result.Provider = Name;

                    result.Item = new Episode
                    {
                        IndexNumber = episodeMatch.Rank,
                        ParentIndexNumber = arc.Rank,
                        Name = episodeMatch.InvariantTitle,
                        //episodes do have release date data
                        PremiereDate = episodeMatch.ReleaseDate,
                        ProductionYear = episodeMatch.ReleaseDate?.Year,
                        Overview = episodeMatch.Description
                    };

                }
                else
                {
                    _log.LogError("Could not find arc {ArcId}", episodeMatch.ArcNum);
                }

            }

            _log.LogInformation(
                    "Identified Episode {Info} --> {March}",
                    JsonSerializer.Serialize(info),
                    JsonSerializer.Serialize(episodeMatch));

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            EpisodeInfo searchInfo,
            CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            var metadataResult = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);
            if (metadataResult.HasMetadata)
            {
                var episode = metadataResult.Item;

                result.Add(new RemoteSearchResult
                {
                    IndexNumber = episode.IndexNumber,
                    ParentIndexNumber = episode.ParentIndexNumber,
                    Name = episode.Name,
                    PremiereDate = episode.PremiereDate,
                    ProductionYear = episode.ProductionYear,
                    Overview = episode.Overview,
                    ProviderIds = episode.ProviderIds,
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

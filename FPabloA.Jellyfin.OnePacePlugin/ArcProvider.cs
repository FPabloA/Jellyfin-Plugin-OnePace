using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;


namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class ArcProvider : IRemoteMetadataProvider<Season, SeasonInfo>, IHasOrder
    {
        private readonly IRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ArcProvider> _log;

        public ArcProvider(IRepository repository, IHttpClientFactory httpClientFactory, ILogger<ArcProvider> logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _log = logger;
        }

        public int Order => -1000;

        public string Name => Plugin.ProviderName;

        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>();

            var arcMatch = await ArcIdentifier.IdentifyAsync(_repository, info, cancellationToken).ConfigureAwait(false);
            if (arcMatch != null)
            {
                result.HasMetadata = true;
                result.Provider = Name;

                result.Item = new Season
                {
                    IndexNumber = arcMatch.Rank,
                    Name = arcMatch.InvariantTitle,
                    Overview = arcMatch.Description
                    //Pretty sure these will not be provided by one pacerr API
                    //PremierDate = arcMatch.ReleaseDate
                    //ProductionYear = arcMatch.ReleaseDate?.Year
                };

                //TODO:removing ArcID stuff
                //result.Item.SetOnePaceId(arcMatch.Id);

                var localization = await _repository
                    .FindBestLocalizationByArcIdAsync(arcMatch.Id, info.MetadataLanguage ?? "en", cancellationToken)
                    .ConfigureAwait(false);
                if (localization != null)
                {
                    result.Item.Name = localization.Title;
                    result.Item.Overview = localization.Description;
                }
            }

            _log.LogInformation(
                "Identified Arc {Info} --> {Match}",
                JsonSerializer.Serialize(info),
                JsonSerializer.Serialize(arcMatch));

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            SeasonInfo searchInfo,
            CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            var metadataResult = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);
            if (metadataResult.HasMetadata)
            {
                var season = metadataResult.Item;

                result.Add(new RemoteSearchResult
                {
                    IndexNumber = season.IndexNumber,
                    Name = season.Name,
                    //PremiereDate = season.PremiereDate,
                    //ProductionYear = season.ProductionYear,
                    Overview = season.Overview,
                    ProviderIds = season.ProviderIds,
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FPabloA.Jellyfin.OnePacePlugin.Model;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

//Provides One Pace Metadata from the OnePacerr API
namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class WebRepository : IRepository
    {

        private const string FallbackLanguageCode = "en";
        //private static string _fallbackApiLanguageCode = ToApiLanguageCode(FallbackLanguageCode);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<WebRepository> _log;

        public WebRepository(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<WebRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _log = logger;
        }

        private async Task<JsonElement> QueryGraphQLAsync(string query, CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreateAsync(query, async cacheEntry =>
            {
                //TODO: Change this to onepacerr API
                var request = new HttpRequestMessage(HttpMethod.Post, "https://onepace.net/api/graphql");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        query
                    }),
                    Encoding.UTF8,
                    "application/json");

                var client = _httpClientFactory.CreateClient(NamedClient.Default);
                var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                //Honor some common caching headers, if present
                var noCache = response.Headers.CacheControl?.NoCache;
                var maxAge = response.Headers.CacheControl?.MaxAge;
                if ((noCache != null && noCache.Value) || maxAge <= TimeSpan.Zero)
                {
                    //Caching is forbidden
                    cacheEntry.AbsoluteExpiration = DateTimeOffset.MinValue;
                }
                else
                {
                    cacheEntry.SlidingExpiration = maxAge;
                    cacheEntry.AbsoluteExpiration = response.Content.Headers.Expires;

                    //fall back to resonable expiration if no explicit expiration was set
                    if (!cacheEntry.SlidingExpiration.HasValue && !cacheEntry.AbsoluteExpiration.HasValue)
                    {
                        cacheEntry.AbsoluteExpiration = DateTimeOffset.MinValue;
                    }
            
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var document = await JsonDocument
                    .ParseAsync(stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return document.RootElement.GetProperty("data");

            }).ConfigureAwait(false);


        }

        private async Task<JsonElement?> FetchMetadataAsync(CancellationToken cancellationToken)
        {
            try
            {
                //TODO: Change this to OnePacerr
                return await QueryGraphQLAsync(
                    @"{series{invariant_title translations{title description language_code}}arcs{id part invariant_title manga_chapters released_at translations{title description language_code}images{src width}episodes{id part invariant_title manga_chapters released_at crc32 translations{title description language_code}images{src width}}}}",
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _log.LogError(ex, "Could not find One Pace metadata, please report a bug at" +
                                      " https://github.com/FPabloA/Jellyfin-Plugin-OnePace if" +
                                      " this happened on the latest version");
                }
                else
                {
                    _log.LogWarning(ex, "Failed to fetch One Pace metadata");
                }
                throw;
            }
        }

        //Not implementing for now since Onepacerr does not use language codes
        //private static string ToApiLanguageCode(string languageCode)
        //{
        //    if (languageCode.Equals("zh"))
        //}

        //private static string ToLanguageCode(string apiLanguageCode)
        //{

        //}

        //private static bool LanguageCodeEqual(string a, string b)
        //{

        //}

        //private static JsonElement? ChooseBestApiTranslation(JsonElement.ArrayEnumerator apiCandidates, string apiLanguageCode)
        //{

        //}

        private async Task<JsonElement?> FindApiArcByIdAsync(string id, CancellationToken cancellationToken)
        {
            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                return apiMetadata?.GetProperty("arc").EnumerateArray().FirstOrNull(apiArc =>
                    apiArc.GetProperty("id").GetNonNullString() == id);
            }
            catch (HttpRequestException)
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }
        }

        private async Task<(string ArcId, JsonElement ApiEpisode)?> FindApiEpisodeByIdAsync(string id, CancellationToken cancellationToken)
        {
            
            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                if (apiMetadata == null)
                {
                    return null;
                }

                foreach (var apiArc in apiMetadata.Value.GetProperty("arcs").EnumerateArray())
                {
                    var matchingEpisode = apiArc.GetProperty("episodes").EnumerateArray()
                        .FirstOrNull(apiEpisode => apiEpisode.GetProperty("id").GetNonNullString() == id);

                    if (matchingEpisode != null)
                    {
                        return (apiArc.GetProperty("id").GetNonNullString(), matchingEpisode.Value);
                    }
                }
                return null;
            }
            catch (HttpRequestException)
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }

        }

        public async Task<ISeries?> FindSeriesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                return apiMetadata != null
                    ? new RepositorySeries(apiMetadata.Value.GetProperty("series"))
                    : null;
            }
            catch (HttpRequestException)
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }
        }

        public async Task<IReadOnlyCollection<IArc>> FindAllArcsAsync(CancellationToken cancellationToken)
        {
            var results = new List<IArc>();

            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                if (apiMetadata != null)
                {
                    results.AddRange(apiMetadata.Value.GetProperty("arcs").EnumerateArray().Select(apiArc =>
                    new RepositoryArc(apiArc)));
                }
            }
            catch (HttpRequestException)
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }
            return results;
        }

        public async Task<IArc?> FindArcByIdAsync(string id, CancellationToken cancellationToken)
        {

            var apiArc = await FindApiArcByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return apiArc != null
                ? new RepositoryArc(apiArc.Value)
                : null;

        }

        public async Task<IReadOnlyCollection<IEpisode>> FindAllEpisodesAsync(CancellationToken cancellationToken)
        {
            var results = new List<IEpisode>();

            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                if (apiMetadata != null)
                {
                    results.AddRange(
                        from apiArc in apiMetadata.Value.GetProperty("arcs").EnumerateArray()
                        from apiEpisode in apiArc.GetProperty("episodes").EnumerateArray()
                        select new RepositoryEpisode(apiArc.GetProperty("id").GetNonNullString(), apiEpisode));
                }
            }
            catch
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }

            return results;
        }

        public async Task<IEpisode?> FindEpisodeByIdAsync(string id, CancellationToken cancellationToken)
        {

            var result = await FindApiEpisodeByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return result != null
                ? new RepositoryEpisode(result.Value.ArcId, result.Value.ApiEpisode)
                : null;

        }

        //One Pacerr doesn't provide image data, so will return null for now

        public Task<IReadOnlyCollection<IArt>> FindAllLogoArtBySeriesAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<IReadOnlyCollection<IArt>> FindAllCoverArtBySeriesAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<IReadOnlyCollection<IArt>> FindAllCoverArtByArcIdAsync(string arcId, CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<IReadOnlyCollection<IArt>> FindAllCoverArtByEpisodeIdAsync(string episodeId, CancellationToken cancellationToken)
        {
            return null;
        }

        //One pacerr doesn't use language codes at all and only provides titles and descriptions in english, will return null for now

        public async Task<ILocalization?> FindBestLocalizationBySeriesAsync(string languageCode, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ILocalization?> FindBestLocalizationByArcIdAsync(string arcId, string languageCode, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ILocalization?> FindBestLocalizationByEpisodeIdAsync(string episodeId, string languageCode, CancellationToken cancellationToken)
        {
            return null;
        }

        private static DateTime? ParseReleaseDate(JsonElement jsonElement)
        {

            var releasedDateString = jsonElement.GetString();
            if (releasedDateString == null || releasedDateString.Equals("unreleased", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return DateTime.Parse(releasedDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        }

        private sealed class RepositorySeries : ISeries
        {
            public RepositorySeries(JsonElement apiSeries)
            {
                InvariantTitle = apiSeries.GetProperty("invariant_title").GetNonNullString();
            }

            public string InvariantTitle { get; }

            public string OriginalTitle => "One Piece";
        }

        private sealed class RepositoryArc : IArc
        {
            public RepositoryArc(JsonElement apiArc)
            {
                Id = apiArc.GetProperty("id").GetNonNullString();
                Rank = apiArc.GetProperty("part").GetInt32();
                InvariantTitle = apiArc.GetProperty("invariant_title").GetNonNullString();
                MangaChapters = apiArc.GetProperty("manga_chapters").GetString();
                ReleaseDate = ParseReleaseDate(apiArc.GetProperty("released_at"));
            }

            public string Id { get; }

            public int Rank { get; }

            public string InvariantTitle { get; }

            public string? MangaChapters { get; }

            public DateTime? ReleaseDate { get; }
        }

        private sealed class RepositoryEpisode : IEpisode
        {
            public RepositoryEpisode(string arcId, JsonElement apiEpisode)
            {
                Id = apiEpisode.GetProperty("id").GetNonNullString();
                Rank = apiEpisode.GetProperty("part").GetInt32();
                ArcId = ArcId;
                InvariantTitle = apiEpisode.GetProperty("invariant_title").GetNonNullString();
                MangaChapters = apiEpisode.GetProperty("manga_chapters").GetString();
                ReleaseDate = ParseReleaseDate(apiEpisode.GetProperty("released_at"));

                var crc32String = apiEpisode.GetProperty("crc32").GetString();
                if (crc32String != null)
                {
                    Crc32 = uint.Parse(crc32String, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
            }

            public string Id { get; }

            public int Rank { get; }

            public string ArcId { get; }

            public string InvariantTitle { get; }

            public string? MangaChapters { get; }

            public DateTime? ReleaseDate { get; }

            public uint? Crc32 { get; }
        }

        //not implementing for now since one pacerr doesnt provide image data

        //private sealed class RepositoryArt : IArt
        //{

        //}

        //This needs to be reworked since one pacerr doesnt use language codes

        //private sealed class RepositoryLocalization : ILocalization
        //{

        //}

    }
}

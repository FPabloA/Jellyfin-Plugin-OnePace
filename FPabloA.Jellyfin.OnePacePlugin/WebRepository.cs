using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private static string _fallbackApiLanguageCode = ToApiLanguageCode(FallbackLanguageCode);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<WebRepository> _log;

        public WebRepository(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<WebRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _log = logger;
        }

        private async Task<JsonElement> QueryAsync(string query, CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreateAsync(query, async cacheEntry =>
            {
                //TODO: still need to change the content portion probably, and this will just be an http get, not graphql query
                var request = new HttpRequestMessage(HttpMethod.Get, query);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {

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

                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var document = await JsonDocument
                    .ParseAsync(stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return document.RootElement;

            }).ConfigureAwait(false);


        }

        private async Task<JsonElement?> FetchMetadataAsync(CancellationToken cancellationToken)
        {
            try
            {
                //TODO: Change this to OnePacerr
                return await QueryAsync(
                    @"https://onepacerr.com/api/v1/metadata/arcs/?episodes=true&files=true",
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
        private static string ToApiLanguageCode(string languageCode)
        {
            if (languageCode.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "zh_cn";
            }

            return languageCode.Replace("-", "_", StringComparison.InvariantCultureIgnoreCase);
        }

        private static string ToLanguageCode(string apiLanguageCode)
        {
            if (apiLanguageCode.Equals("zh_cn", StringComparison.OrdinalIgnoreCase))
            {
                return "zh";
            }

            return apiLanguageCode.Replace("_", "-", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool LanguageCodesEqual(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static JsonElement? ChooseBestApiTranslation(JsonElement.ArrayEnumerator apiCandidates, string apiLanguageCode)
        {
            while (true)
            {
                foreach (var apiCandidate in apiCandidates)
                {
                    if (LanguageCodesEqual(apiCandidate.GetProperty("language_code").GetNonNullString(), apiLanguageCode))
                    {
                        return apiCandidate;
                    }
                }

                // Do we have anything to fall back on?
                if (LanguageCodesEqual(apiLanguageCode, _fallbackApiLanguageCode))
                {
                    return null;
                }

                apiLanguageCode = _fallbackApiLanguageCode;
            }
        }

        //TODO:Clearing stuff to do with arcID
        //private async Task<JsonElement?> FindApiArcByIdAsync(string id, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
        //        return apiMetadata?.GetProperty("arcs").EnumerateArray().FirstOrNull(apiArc =>
        //            apiArc.GetProperty("id").GetNonNullString() == id);
        //    }
        //    catch (HttpRequestException)
        //    {
        //        //Details should be logged futher down the stack. just treat the data as unavailable
        //        return null;
        //    }
        //}

        private async Task<JsonElement?> FindApiArcByNumberAsync(string arcNum, CancellationToken cancellationToken)
        {
            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                return apiMetadata?.GetProperty("arcs").EnumerateArray().FirstOrNull(apiArc =>
                    apiArc.GetProperty("arc").GetNonNullString() == arcNum);
            }
            catch (HttpRequestException)
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }
        }

        //TODO: Clearing EpisodeID stuff
        //private async Task<(string ArcId, JsonElement ApiEpisode)?> FindApiEpisodeByIdAsync(string id, CancellationToken cancellationToken)
        //{

        //    try
        //    {
        //        var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
        //        if (apiMetadata == null)
        //        {
        //            return null;
        //        }

        //        foreach (var apiArc in apiMetadata.Value.GetProperty("arcs").EnumerateArray())
        //        {
        //            var matchingEpisode = apiArc.GetProperty("episodes").EnumerateArray()
        //                .FirstOrNull(apiEpisode => apiEpisode.GetProperty("id").GetNonNullString() == id);

        //            if (matchingEpisode != null)
        //            {
        //                return (apiArc.GetProperty("arc").ToString(), matchingEpisode.Value);
        //            }
        //        }
        //        return null;
        //    }
        //    catch (HttpRequestException)
        //    {
        //        //Details should be logged futher down the stack. just treat the data as unavailable
        //        return null;
        //    }

        //}

        public async Task<ISeries?> FindSeriesAsync(CancellationToken cancellationToken)
        {
            try
            {
                //Attempting to hardcode series data instead of expecting it from API
                //var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                //return apiMetadata != null
                //    ? new RepositorySeries(apiMetadata.Value.GetProperty("series"))
                //    : null;
                return new RepositorySeries("One Pace");
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
                    results.AddRange(apiMetadata.Value.EnumerateArray().Select(apiArc =>
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

        //TODO:Clearing stuff to do with arcID
        //public async Task<IArc?> FindArcByIdAsync(string id, CancellationToken cancellationToken)
        //{

        //    var apiArc = await FindApiArcByIdAsync(id, cancellationToken).ConfigureAwait(false);
        //    return apiArc != null
        //        ? new RepositoryArc(apiArc.Value)
        //        : null;

        //}

        public async Task<IArc?> FindArcByNumberAsync(string arcNum, CancellationToken cancellationToken)
        {

            var apiArc = await FindApiArcByNumberAsync(arcNum, cancellationToken).ConfigureAwait(false);
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
                        from apiArc in apiMetadata.Value.EnumerateArray()
                        from apiEpisode in apiArc.GetProperty("episodes").EnumerateArray()
                        select new RepositoryEpisode(apiArc.GetProperty("arc").ToString(), apiEpisode));
                }
            }
            catch
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }

            return results;
        }

        //TODO: clearing episodeid stuff
        //public async Task<IEpisode?> FindEpisodeByIdAsync(string id, CancellationToken cancellationToken)
        //{

        //    var result = await FindApiEpisodeByIdAsync(id, cancellationToken).ConfigureAwait(false);
        //    return result != null
        //        ? new RepositoryEpisode(result.Value.ArcId, result.Value.ApiEpisode)
        //        : null;

        //}

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

        //public Task<IReadOnlyCollection<IArt>> FindAllCoverArtByEpisodeIdAsync(string episodeId, CancellationToken cancellationToken)
        //{
        //    return null;
        //}

        //One pacerr doesn't use language codes at all and only provides titles and descriptions in english, will need to rework this to just return en probably

        public async Task<ILocalization?> FindBestLocalizationBySeriesAsync(string languageCode, CancellationToken cancellationToken)
        {
            try
            {
                var apiMetadata = await FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
                var apiSeries = apiMetadata?.GetProperty("series");

                var bestApiTranslation = apiSeries != null
                    ? ChooseBestApiTranslation(
                        apiSeries.Value.GetProperty("translations").EnumerateArray(),
                        ToApiLanguageCode(languageCode))
                    : null;

                return bestApiTranslation != null
                    ? new RepositoryLocalization(bestApiTranslation.Value)
                    : null;
            }
            catch (HttpRequestException)
            {
                // Details should have been logged further down the stack. We just treat this data as unavailable for now
                // and the user can try again manually if they want.
                return null;
            }
        }

        //TODO:Clearing stuff to do with arcID
        //public async Task<ILocalization?> FindBestLocalizationByArcIdAsync(string arcId, string languageCode, CancellationToken cancellationToken)
        //{
        //    var apiArc = await FindApiArcByIdAsync(arcId, cancellationToken).ConfigureAwait(false);
        //    if (apiArc != null)
        //    {
        //        var bestApiTranslation = ChooseBestApiTranslation(
        //            apiArc.Value.GetProperty("translations").EnumerateArray(),
        //            ToApiLanguageCode(languageCode));

        //        if (bestApiTranslation != null)
        //        {
        //            return new RepositoryLocalization(bestApiTranslation.Value);
        //        }
        //    }

        //    return null;
        //}

        //TODO: Clearing stuff to do with EpisodeID
        //public async Task<ILocalization?> FindBestLocalizationByEpisodeIdAsync(string episodeId, string languageCode, CancellationToken cancellationToken)
        //{
        //    var result = await FindApiEpisodeByIdAsync(episodeId, cancellationToken)
        //    .ConfigureAwait(false);
        //    if (result == null)
        //    {
        //        return null;
        //    }

        //    var apiTranslation = ChooseBestApiTranslation(
        //        result.Value.ApiEpisode.GetProperty("translations").EnumerateArray(),
        //        ToApiLanguageCode(languageCode));

        //    return apiTranslation != null
        //        ? new RepositoryLocalization(apiTranslation.Value)
        //        : null;
        //}

        private static DateTime? ParseReleaseDate(JsonElement jsonElement)
        {

            var releasedDateString = jsonElement.GetString();
            if (releasedDateString == null || releasedDateString.Equals("unreleased", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return DateTime.Parse(releasedDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        }

        //private sealed class RepositorySeries : ISeries
        //{
        //    public RepositorySeries(JsonElement apiSeries)
        //    {
        //        InvariantTitle = apiSeries.GetProperty("invariant_title").GetNonNullString();
        //    }

        //    public string InvariantTitle { get; }

        //    public string OriginalTitle => "One Piece";
        //}
        
        //Attempting to move away from needing series in metadata JSON
        private sealed class RepositorySeries : ISeries
        {
            public RepositorySeries(string title)
            {
                InvariantTitle = title;
            }

            public string InvariantTitle { get; }

            public string OriginalTitle => "One Piece";
        }

        private sealed class RepositoryArc : IArc
        {
            public RepositoryArc(JsonElement apiArc)
            {
                //TODO: Clearing stuff with arcID
                //Id = apiArc.GetProperty("id").GetNonNullString();
                Rank = apiArc.GetProperty("arc").GetInt32();
                InvariantTitle = apiArc.GetProperty("title").GetNonNullString();
                //If content is anime only, there will be no mangaChapters property, need to check
                if (apiArc.TryGetProperty("mangaChapters", out _))
                {
                    MangaChapters = apiArc.GetProperty("mangaChapters").GetString();
                }
                
                Description = apiArc.GetProperty("description").GetString();
                //ReleaseDate = ParseReleaseDate(apiArc.GetProperty("released_at"));
            }

            //TODO: Clearing stuff with arcID
            //public string Id { get; }

            public int Rank { get; }

            public string InvariantTitle { get; }

            public string? MangaChapters { get; }

            public string Description { get; }

            //public DateTime? ReleaseDate { get; }
        }

        private sealed class RepositoryEpisode : IEpisode
        {
            public RepositoryEpisode(string arcNum, JsonElement apiEpisode)
            {
                //TODO: Clearing stuff to do with EpisodeID
                //Id = apiEpisode.GetProperty("id").GetNonNullString();
                Rank = apiEpisode.GetProperty("episode").GetInt32();
                //TODO: Clearing stuff with arcID
                //ArcId = ArcId;
                ArcNum = arcNum;
                InvariantTitle = apiEpisode.GetProperty("title").GetNonNullString();
                if (apiEpisode.TryGetProperty("mangaChapters", out _))
                {
                    MangaChapters = apiEpisode.GetProperty("mangaChapters").GetString();
                }
                if (apiEpisode.TryGetProperty("released", out _))
                {
                    ReleaseDate = ParseReleaseDate(apiEpisode.GetProperty("released"));
                }
                Description = apiEpisode.GetProperty("description").GetString();

                //var crc32String = apiEpisode.GetProperty("crc32").GetString();
                //if (crc32String != null)
                //{
                //    Crc32 = uint.Parse(crc32String, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                //}

                //Use this when switching to one pacerr crc testing
                var crc32String = apiEpisode.GetProperty("files").GetProperty("standard").GetProperty("CRC32").GetString();
                if (crc32String != null)
                {
                    Crc32 = uint.Parse(crc32String, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
            }

            //TODO: Clearing stuff to do with EpisodeID
            //public string Id { get; }

            public int Rank { get; }

            //TODO: Clearing stuff with arcID
            //public string ArcId { get; }

            public string ArcNum { get; }

            public string InvariantTitle { get; }

            public string? MangaChapters { get; }

            public DateTime? ReleaseDate { get; }

            public uint? Crc32 { get; }

            public string Description { get; }
        }

        //not implementing for now since one pacerr doesnt provide image data

        //private sealed class RepositoryArt : IArt
        //{

        //}

        //This needs to be reworked since one pacerr doesnt use language codes

        private sealed class RepositoryLocalization : ILocalization
        {
            public RepositoryLocalization(JsonElement apiTranslation)
            {
                LanguageCode = ToLanguageCode(apiTranslation.GetProperty("language_code").GetNonNullString());
                Title = apiTranslation.GetProperty("title").GetNonNullString();
                Description = apiTranslation.GetProperty("description").GetString();
            }

            public string LanguageCode { get; }

            public string Title { get; }

            public string? Description { get; }
        }

    }
}
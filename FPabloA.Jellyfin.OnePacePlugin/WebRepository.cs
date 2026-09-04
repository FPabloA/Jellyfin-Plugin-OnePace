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
                        select new RepositoryEpisode(apiArc.GetProperty("arc").ToString(), apiArc.GetProperty("title").ToString(), apiEpisode));
                }
            }
            catch
            {
                //Details should be logged futher down the stack. just treat the data as unavailable
                return null;
            }

            return results;
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

        private static DateTime? ParseReleaseDate(JsonElement jsonElement)
        {

            var releasedDateString = jsonElement.GetString();
            if (releasedDateString == null || releasedDateString.Equals("unreleased", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return DateTime.Parse(releasedDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        }

        private static string RebuildFileTitle(string arcTitle, int episodeNum)
        {
            string padEpisode;

            if (episodeNum < 10)
            {
                padEpisode = "0" + episodeNum;
            }
            else
            {
                padEpisode = episodeNum.ToString();
            }
            return $"{arcTitle} {padEpisode}";
        }
        
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
                Rank = apiArc.GetProperty("arc").GetInt32();
                InvariantTitle = apiArc.GetProperty("title").GetNonNullString();
                //If content is anime only, there will be no mangaChapters property, need to check
                if (apiArc.TryGetProperty("mangaChapters", out _))
                {
                    MangaChapters = apiArc.GetProperty("mangaChapters").GetString();
                }
                
                Description = apiArc.GetProperty("description").GetString();
            }



            public int Rank { get; }

            public string InvariantTitle { get; }

            public string? MangaChapters { get; }

            public string Description { get; }
        }

        private sealed class RepositoryEpisode : IEpisode
        {
            public RepositoryEpisode(string arcNum, string arcName, JsonElement apiEpisode)
            {
                Rank = apiEpisode.GetProperty("episode").GetInt32();
                ArcNum = arcNum;
                InvariantTitle = apiEpisode.GetProperty("title").GetNonNullString();
                FileTitle = RebuildFileTitle(arcName, Rank);
                if (apiEpisode.TryGetProperty("mangaChapters", out _))
                {
                    MangaChapters = apiEpisode.GetProperty("mangaChapters").GetString();
                }
                if (apiEpisode.TryGetProperty("released", out _))
                {
                    ReleaseDate = ParseReleaseDate(apiEpisode.GetProperty("released"));
                }
                Description = apiEpisode.GetProperty("description").GetString();

                //Use this when switching to one pacerr crc testing
                var crc32String = apiEpisode.GetProperty("files").GetProperty("standard").GetProperty("CRC32").GetString();
                if (crc32String != null)
                {
                    Crc32 = uint.Parse(crc32String, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
            }

            public int Rank { get; }

            public string ArcNum { get; }

            public string InvariantTitle { get; }

            public string FileTitle { get; }

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
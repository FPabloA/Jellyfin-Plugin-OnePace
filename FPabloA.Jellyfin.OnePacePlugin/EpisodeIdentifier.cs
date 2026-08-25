using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using FPabloA.Jellyfin.OnePacePlugin.Model;

namespace FPabloA.Jellyfin.OnePacePlugin
{
    internal static class EpisodeIdentifier
    {
        public static async Task<IEpisode?> IdentifyAsync(
            IRepository repository,
            ItemLookupInfo itemLookupInfo,
            CancellationToken cancellationToken)
        {
            //attempt to retrieve metadata using the episode ID (probably not relevant to this)
            var episodeId = itemLookupInfo.GetOnePaceId();
            if (episodeId != null)
            {
                var episodeInfo = await repository
                    .FindEpisodeByIdAsync(episodeId, cancellationToken)
                    .ConfigureAwait(false);
                if (episodeInfo != null)
                {
                    return episodeInfo;
                }
            }

            if (itemLookupInfo.Path != null && IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(itemLookupInfo.Path))
            {
                var episodes = await repository.FindAllEpisodesAsync(cancellationToken).ConfigureAwait(false);

                // All of these file names should get matched properly:
                // - "[One Pace][3-5] Romance Dawn 03 [1080p][D767799C]" (case 1)
                // - "Romance Dawn 03" (case 3)
                // - "3-5" (case 2)
                var fileName = Path.GetFileNameWithoutExtension(itemLookupInfo.Path);

                //match against CRC-32 (Case 1)
                foreach (var episode in episodes)
                {
                    if (episode.Crc32 != null)
                    {
                        var pattern = $@"\b{episode.Crc32.Value:XB}\b";
                        if (Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase))
                        {
                            return episode;
                        }
                    }
                }

                //match against chapter ranges (case 2)
                foreach (var episode in episodes.OrderByDescending(episode => episode.MangaChapters?.Length ?? 0))
                {
                    if (!string.IsNullOrEmpty(episode.MangaChapters) && IdentifierUtil.BuildTextRegex(episode.MangaChapters).IsMatch(fileName))
                    {
                        return episode;
                    }
                }

                //match against invariant titles (case 3)
                foreach (var episode in episodes.OrderByDescending(episode => episode.InvariantTitle.Length))
                {
                    if (!string.IsNullOrEmpty(episode.InvariantTitle) && IdentifierUtil.BuildTextRegex(episode.InvariantTitle).IsMatch(fileName))
                    {
                        return episode;
                    }
                }

            }

            return null;
        }
    }
}

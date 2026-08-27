using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FPabloA.Jellyfin.OnePacePlugin.Model;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;

//Fetch arc information

namespace FPabloA.Jellyfin.OnePacePlugin
{
    internal static class ArcIdentifier
    {
        public static async Task<IArc?> IdentifyAsync(
            IRepository repository,
            ItemLookupInfo itemLookupInfo,
            CancellationToken cancellationToken)
        {
            //try to find by arc id first (might not work as onepacerr does not have an id field)
            var arcId = itemLookupInfo.GetOnePaceId();
            if (arcId != null)
            {
                //attempt to find by id, If successful return the arc
                var arc = await repository.FindArcByIdAsync(arcId, cancellationToken).ConfigureAwait(false);
                if (arc != null)
                {
                    return arc;
                }
            }

            if (itemLookupInfo.Path != null && IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(itemLookupInfo.Path))
            {
                var arcs = await repository.FindAllArcsAsync(cancellationToken).ConfigureAwait(false);

                // All of these folder names should get matched properly:
                // - "[One Pace][1-7] Romance Dawn [1080p]" (Case 1)
                // - "Arc 1 - Romance Dawn" (Case 2 Or 3)
                // - "Romance Dawn" (Case 2)
                // - "1" (Case 3)

                var directoryName = Path.GetFileName(itemLookupInfo.Path);

                //match using chapter ranges (Case 1)
                foreach(var arc in arcs.OrderByDescending(arc => arc.MangaChapters?.Length ?? 0))
                {
                    //if chapter range exists and is found in both the retrieved arc and the directory name return the arc
                    if (!string.IsNullOrEmpty(arc.MangaChapters) && IdentifierUtil.BuildTextRegex(arc.MangaChapters).IsMatch(directoryName))
                    {
                        return arc;
                    }
                }

                //match against invariant titles (Case 2)
                foreach (var arc in arcs.OrderByDescending(arc => arc.InvariantTitle.Length))
                {
                    //if the title exists and is found in both the retrieved arc and the directory name return the arc
                    if(!string.IsNullOrEmpty(arc.InvariantTitle) && IdentifierUtil.BuildTextRegex(arc.InvariantTitle).IsMatch(directoryName))
                    {
                        return arc;
                    }
                }

                //match against arc ranks (Case 3)
                foreach (var arc in arcs)
                {
                    //setup regex for checking rank
                    var pattern = @"\b0*" + Regex.Escape(arc.Rank.ToString(CultureInfo.InvariantCulture)) + @"\b";
                    //if a rank match is found return the arc
                    if(Regex.IsMatch(directoryName, pattern, RegexOptions.IgnoreCase))
                    {
                        return arc;
                    }
                }
            }

            //none of the cases could identify
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPabloA.Jellyfin.OnePacePlugin.Model
{
    //Represents a series episode
    public interface IEpisode
    {
        //Gets the CUID for the Episode (might not be relevant anymore, unless this is for jellyfin purposes and not one pace)
        string Id { get; }

        ////Gets the CUID of the arc that the episode belongs to (if the arc CUID is not relevant, then this will be removed too)
        //TODO: Clearing ArcId stuff
        //string ArcId { get; }

        //Gets the arc number that the episode belongs to
        string ArcNum { get; }

        //Gets the rank/order of the episode within the arc
        int Rank { get; }

        //Gets the invariant title of the episode, e.g. "Romance Dawn 01"
        string InvariantTitle { get; }

        //Gets the manga chapters covered by the episode. Null if unknown, or if episode is anime-only
        string? MangaChapters { get; }

        //Gets the release date of the episode. Null if release date is unknown or otherwise unavailable (API provides in form of a string)
        DateTime? ReleaseDate { get; }

        //Gets the CRC-32 checksum of the episode file. Null if unknown or otherwise unavailable
        uint? Crc32 { get; }
    }
}

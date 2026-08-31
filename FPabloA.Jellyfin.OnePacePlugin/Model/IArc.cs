using System;

namespace FPabloA.Jellyfin.OnePacePlugin.Model
{
    //Represents a series arc
    public interface IArc
    {
        //Gets the CUID for the Arc (might not be relevant anymore, unless this is for jellyfin purposes and not one pace)
        string Id { get; }

        //Gets the rank/order of the arc within the series
        int Rank { get; }

        //Gets the invariant title of the arc, e.g. "Romance Dawn"
        string InvariantTitle { get; }

        //Gets the manga chapters being covered by the arc. Null if Unknown, or if Arc is Anime-Only
        string? MangaChapters { get; }

        //Gets the release date of the arc. Null if release date is unknown, or otherwise unavailable (might not be provided by onepacerr API, could remove)
        //DateTime? ReleaseDate { get; }

    }
}
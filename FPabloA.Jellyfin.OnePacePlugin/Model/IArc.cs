using System;

namespace FPabloA.Jellyfin.OnePacePlugin.Model
{
    //Represents a series arc
    public interface IArc
    {
        //Gets the rank/order of the arc within the series
        int Rank { get; }

        //Gets the invariant title of the arc, e.g. "Romance Dawn"
        string InvariantTitle { get; }

        //Gets the manga chapters being covered by the arc. Null if Unknown, or if Arc is Anime-Only
        string? MangaChapters { get; }

        //Gets the english description for the arc
        string Description { get; }

    }
}
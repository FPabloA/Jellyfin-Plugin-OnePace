
namespace FPabloA.Jellyfin.OnePacePlugin.Model
{
    //Represents artwork for either a series, arc, or episode. (Images are definitely not provided by onepacerr API, might remove this later)
    internal interface IArt
    {
        //Gets the URL of the artwork.
        string URL { get; }

        //Gets the width of the image in pixels, Null if unknown
        int? Width { get; }

        //Gets the height of the image in pixels, Null if unknown
        int? Height { get; }
    }
}

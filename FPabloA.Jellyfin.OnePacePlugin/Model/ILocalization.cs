
namespace FPabloA.Jellyfin.OnePacePlugin.Model
{
    //Represents the localization for either a series, arc, or an episode (OnePacerr API doesn't seem to provide data like language codes)
    public interface ILocalization
    {
        //Gets the ISO 639-1 language code for the content
        string LanguageCode { get; }

        //Gets the title of the content in the respective language (might not be relevant if onepacerr only provides titles in english)
        string Title { get; }

        //Gets the description of the content in the respective language. Null if no description is provided (might not be relevant if onepacerr only provides descriptions in english)
        string? Description { get; }
    }
}

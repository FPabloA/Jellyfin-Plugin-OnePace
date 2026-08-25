using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;


namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class SeriesExternalId : IExternalId
    {
        public string ProviderName => Plugin.ProviderName;

        public string Key => Plugin.ProviderName;

        public ExternalIdMediaType? Type => null;

        public string UrlFormatString => "https://onepace.net";

        public bool Supports(IHasProviderIds item) => item is Series;
    }
}

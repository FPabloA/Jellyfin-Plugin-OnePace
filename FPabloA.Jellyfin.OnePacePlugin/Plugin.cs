using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;

namespace FPabloA.Jellyfin.OnePacePlugin
{
    //The main plugin
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        internal const string ProviderName = "One Pace";

        //Use a dummy ID for the series ID
        internal const string DummySeriesId = "clkspj4vn000008k33lnnb4hj";

        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        public override string Name => ProviderName;

        public override Guid Id => Guid.Parse("46E757F6-575F-4DD5-8AFD-DB6976C832CF");

        public override string Description => "Plugin for setting metadata for the One Pace fan edit of the original One Piece series";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new List<PluginPageInfo>();
        }
    }
}
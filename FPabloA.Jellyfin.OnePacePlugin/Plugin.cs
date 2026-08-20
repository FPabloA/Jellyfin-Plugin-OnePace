using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

        public override Guid Id => Guid.Parse("46E757F6-575F-4DD5-8AFD-DB6976C832CF");

        public override string Name => "My Plugin";

        public override string Description => "Does something awesome with Jellyfin";
    }
}
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;


namespace FPabloA.Jellyfin.OnePacePlugin
{
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IRepository, WebRepository>();
        }
    }
}

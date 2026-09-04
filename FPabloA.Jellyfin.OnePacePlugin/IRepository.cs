using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FPabloA.Jellyfin.OnePacePlugin.Model;

namespace FPabloA.Jellyfin.OnePacePlugin
{
    //Provides One Pace metadata
    public interface IRepository
    {
        //Retrieves the series model
        Task<ISeries?> FindSeriesAsync(CancellationToken cancellationToken);

        //Retrieves the models for all known arcs. Read-Only (not sure how this is used yet)
        Task<IReadOnlyCollection<IArc>> FindAllArcsAsync(CancellationToken cancellationToken);

        //Retrieves the arc model based on the arc number
        Task<IArc?> FindArcByNumberAsync(string arcNum, CancellationToken cancellationToken);

        //Retrieves the models for all known episodes. Read-Only (not sure how this is used yet)
        Task<IReadOnlyCollection<IEpisode>> FindAllEpisodesAsync(CancellationToken cancellationToken);

        //Retrieves the available series logo art (probably not relevant since onepacerr does not provide image info)
        Task<IReadOnlyCollection<IArt>> FindAllLogoArtBySeriesAsync(CancellationToken cancellationToken);

        //Retrieves the available series cover art (probably not relevant since onepacerr does not provide image info)
        Task<IReadOnlyCollection<IArt>> FindAllCoverArtBySeriesAsync(CancellationToken cancellationToken);

        //Retrieves the series localization data (might not be relevant since onepacerr appears to only provide description and title info in one language)
        Task<ILocalization?> FindBestLocalizationBySeriesAsync(string languageCode, CancellationToken cancellationToken);
    }
}

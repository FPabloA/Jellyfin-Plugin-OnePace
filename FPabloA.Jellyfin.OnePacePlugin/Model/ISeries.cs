using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPabloA.Jellyfin.OnePacePlugin.Model
{
    //Represents a series
    public interface ISeries
    {
        //Gets the invariant title of the series, e.g. "One Pace"
        string InvariantTitle { get; }

        //Gets the original title of the series, e.g. "One Piece"
        string OriginalTitle { get; }

    }
}

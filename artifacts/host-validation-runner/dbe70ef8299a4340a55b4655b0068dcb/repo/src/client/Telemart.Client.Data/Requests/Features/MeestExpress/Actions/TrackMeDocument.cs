using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Data.Requests.Features.MeestExpress.Actions
{
    public sealed class TrackMeDocument : CallEntityActionRequestResultBase<List<MeTrackDto>>
    {
        public TrackMeDocument(string trackNumber)
            : base(trackNumber, ApiResources.MeestExpressDocuments, "track")
        {
        }
    }
}
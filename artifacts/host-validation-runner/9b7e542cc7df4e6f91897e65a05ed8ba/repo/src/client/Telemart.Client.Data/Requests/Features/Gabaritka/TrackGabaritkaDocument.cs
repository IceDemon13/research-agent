using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Gabaritka;

namespace Telemart.Client.Data.Requests.Features.Gabaritka
{
    public sealed class TrackGabaritkaDocument : CallEntityActionRequestResultBase<GabaritkaTrackNumberDto>
    {
        public TrackGabaritkaDocument(string trackNumber)
            : base(trackNumber, ApiResources.GabaritkaDocuments, "track")
        {
        }
    }
}
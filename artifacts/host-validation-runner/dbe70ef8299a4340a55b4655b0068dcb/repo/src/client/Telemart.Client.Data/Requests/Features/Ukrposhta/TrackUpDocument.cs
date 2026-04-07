using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class TrackUpDocument : CallEntityActionRequestResultBase<List<UpActionsStatusDto>>
    {
        public TrackUpDocument(string barcode)
            : base(barcode, ApiResources.UkrposhtaDocuments, "track")
        {
        }
    }
}
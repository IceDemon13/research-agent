using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Teks;

namespace Telemart.Client.Data.Requests.Features.Teks
{
    public sealed class GetInfoTeksDocuments : CallActionWithBodyRequestResultBase<TtnStatusDto[], TrackingTeksDocumentsDto>
    {
        public GetInfoTeksDocuments(params string[] invoice)
            : base(new TrackingTeksDocumentsDto { InvoiceNumbers = invoice }, ApiResources.TeksDocuments, "history_ttn")
        {
        }
    }
}
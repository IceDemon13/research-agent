using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class QueryNewPostDocumentDeliveryDate : CallActionWithBodyRequestBase<NewPostGetDeliveryDateResponse, NewPostGetDeliveryDateRequest>
    {
        public QueryNewPostDocumentDeliveryDate(NewPostGetDeliveryDateRequest request)
            : base(request, ApiResources.NovaposhtaDocuments, "get_delivery_date")
        {
        }
    }
}
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class CreateUpTtnByOrder : CallActionWithBodyRequestResultBase<UpDocumentDto, UkrposhtaTtnCreateByOrderDto>
    {
        public CreateUpTtnByOrder(UkrposhtaTtnCreateByOrderDto request)
            : base(request, ApiResources.UkrposhtaDocuments, "create_by_order")
        {
        }
    }
}
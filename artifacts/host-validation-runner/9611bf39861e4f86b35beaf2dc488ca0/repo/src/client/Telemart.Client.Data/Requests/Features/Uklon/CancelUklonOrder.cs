using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Uklon
{
    public sealed class CancelUklonOrder : CallEntityActionWithBodyRequestResultBase<object, CancelUklonOrderDto>
    {
        public CancelUklonOrder(string uklonDocumentId, CancelUklonOrderDto dto)
            : base(uklonDocumentId, dto, $"{ApiResources.Uklon}/document", "cancel")
        {
        }
    }
}
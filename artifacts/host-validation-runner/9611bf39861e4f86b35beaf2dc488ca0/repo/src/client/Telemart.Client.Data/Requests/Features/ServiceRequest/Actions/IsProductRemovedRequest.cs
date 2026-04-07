using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public class IsProductRemovedRequest : CallActionWithBodyRequestResultBase<object, IsProductRemovedDto>
    {
        public IsProductRemovedRequest(IsProductRemovedDto dto)
            : base(dto, ApiResources.ServiceRequests, "is_product_removed")
        {
        }
    }
}

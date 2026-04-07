using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class GiveOnRepairServiceProduct : CallEntityActionRequestResultBase<ServiceProductDto>
    {
        public GiveOnRepairServiceProduct(int serviceProductId)
            : base(serviceProductId, ApiResources.ServiceProducts, "repair")
        {
        }
    }
}
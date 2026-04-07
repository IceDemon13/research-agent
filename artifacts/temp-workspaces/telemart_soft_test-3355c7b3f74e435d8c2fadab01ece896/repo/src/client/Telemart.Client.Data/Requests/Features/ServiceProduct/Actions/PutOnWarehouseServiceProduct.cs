using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    // TODO: Rewrite to method with body
    public sealed class PutOnWarehouseServiceProduct : CallEntityActionRequestResultBase<ServiceProductDto>
    {
        public PutOnWarehouseServiceProduct(int serviceProductId, int warehouseId)
            : base(serviceProductId, ApiResources.ServiceProducts,  $"store/{warehouseId}")
        {
        }
    }
}
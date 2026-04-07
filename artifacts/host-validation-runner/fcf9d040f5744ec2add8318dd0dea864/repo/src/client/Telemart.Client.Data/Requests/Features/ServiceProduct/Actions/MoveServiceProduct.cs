using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class MoveServiceProduct : CallEntityActionWithBodyRequestResultBase<ServiceProductDto, ServiceProductMoveDto>
    {
        public MoveServiceProduct(int serviceProductId, int warehouseId)
            : base(serviceProductId, new ServiceProductMoveDto(serviceProductId, warehouseId), ApiResources.ServiceProducts, "move")
        {
        }
    }
}
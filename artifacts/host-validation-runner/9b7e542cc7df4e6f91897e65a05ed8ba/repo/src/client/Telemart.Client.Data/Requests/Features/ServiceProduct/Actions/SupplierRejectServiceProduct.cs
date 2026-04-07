using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class SupplierRejectServiceProduct : CallEntityActionRequestResultBase<ServiceProductDto>
    {
        public SupplierRejectServiceProduct(int serviceProductId)
            : base(serviceProductId, ApiResources.ServiceProducts, "supplier_reject")
        {
        }
    }
}
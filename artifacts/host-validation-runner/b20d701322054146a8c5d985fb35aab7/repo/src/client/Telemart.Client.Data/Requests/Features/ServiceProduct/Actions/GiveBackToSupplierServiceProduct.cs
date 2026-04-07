using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class GiveBackToSupplierServiceProduct : CallEntityActionRequestResultBase<ServiceProductDto>
    {
        public GiveBackToSupplierServiceProduct(int serviceProductId)
        : base(serviceProductId, ApiResources.ServiceProducts, "return")
        {
        }
    }
}
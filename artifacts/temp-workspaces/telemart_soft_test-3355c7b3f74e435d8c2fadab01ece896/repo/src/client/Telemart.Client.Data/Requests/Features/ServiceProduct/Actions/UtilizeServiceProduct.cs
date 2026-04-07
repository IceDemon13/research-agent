using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public class UtilizeServiceProduct : CallEntityActionRequestResultBase<ServiceProductDto>
    {
        public UtilizeServiceProduct(int serviceProductId)
            : base(serviceProductId, ApiResources.ServiceProducts, "utilize")
        {
        }
    }
}
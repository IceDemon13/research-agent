using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class StopDoingAdditionalServiceProduct : CallEntityActionRequestResultBase<AdditionalServiceProductDto>
    {
        public StopDoingAdditionalServiceProduct(int additionalServiceProductId)
            : base(additionalServiceProductId, ApiResources.AdditionalServicesProducts, "stop_doing")
        {
        }
    }
}
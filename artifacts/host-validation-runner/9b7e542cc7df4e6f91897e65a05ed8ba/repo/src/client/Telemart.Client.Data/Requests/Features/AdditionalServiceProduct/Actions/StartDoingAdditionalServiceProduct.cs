using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class StartDoingAdditionalServiceProduct : CallEntityActionRequestResultBase<AdditionalServiceProductDto>
    {
        public StartDoingAdditionalServiceProduct(int additionalServiceProductId)
            : base(additionalServiceProductId, ApiResources.AdditionalServicesProducts, "start_doing")
        {
        }
    }
}
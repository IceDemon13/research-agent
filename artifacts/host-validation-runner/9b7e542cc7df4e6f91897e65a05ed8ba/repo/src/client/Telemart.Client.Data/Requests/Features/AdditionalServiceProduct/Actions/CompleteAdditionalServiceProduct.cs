using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class CompleteAdditionalServiceProduct : CallEntityActionRequestResultBase<AdditionalServiceProductDto>
    {
        public CompleteAdditionalServiceProduct(int additionalServiceProductId)
            : base(additionalServiceProductId, ApiResources.AdditionalServicesProducts, "complete")
        {
        }
    }
}
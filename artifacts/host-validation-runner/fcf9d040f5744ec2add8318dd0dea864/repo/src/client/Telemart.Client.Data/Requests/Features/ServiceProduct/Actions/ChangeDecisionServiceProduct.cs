using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class ChangeDecisionServiceProduct : CallEntityActionRequestResultBase<ServiceProductDto>
    {
        public ChangeDecisionServiceProduct(int id)
            : base(id, ApiResources.ServiceProducts, "change_decision")
        {
        }
    }
}
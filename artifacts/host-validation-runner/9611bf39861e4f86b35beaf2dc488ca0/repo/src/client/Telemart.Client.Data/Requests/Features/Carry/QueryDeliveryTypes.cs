using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class QueryDeliveryTypes : QueryEntitiesRequestBase<DeliveryTypeDto>
    {
        public QueryDeliveryTypes()
            : base($"{ApiResources.Carries}/{ApiResources.DeliveryTypes}")
        {
        }
    }
}
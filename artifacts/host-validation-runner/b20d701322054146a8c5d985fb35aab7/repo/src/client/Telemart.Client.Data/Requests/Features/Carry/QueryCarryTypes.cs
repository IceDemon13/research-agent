using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class QueryCarryTypes : QueryEntitiesRequestBase<CarryTypeDto>
    {
        public QueryCarryTypes()
            : base($"{ApiResources.Carries}/types")
        {
        }
    }
}
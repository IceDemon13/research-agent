using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class QueryCarryProviders : QueryEntitiesRequestBase<CarryProviderDto>
    {
        public QueryCarryProviders()
            : base($"{ApiResources.Carries}/providers")
        {
        }
    }
}
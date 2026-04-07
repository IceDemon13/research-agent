using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Logistics
{
    public sealed class QueryExpireReasons : QueryEntitiesRequestBase<ReasonExpireDto>
    {
        public QueryExpireReasons()
            : base($"{ApiResources.Logistics}/expire_reasons")
        {
        }
    }
}
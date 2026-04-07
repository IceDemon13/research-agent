using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.Monitoring
{
    public sealed class QueryActiveClients : QueryEntitiesRequestBase<ActiveClientDto>
    {
        public QueryActiveClients()
            : base(ApiResources.ActiveClients)
        {
        }
    }
}
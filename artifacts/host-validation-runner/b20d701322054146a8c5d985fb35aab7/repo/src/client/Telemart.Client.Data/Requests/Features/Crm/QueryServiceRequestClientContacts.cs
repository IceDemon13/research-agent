using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Crm
{
    public sealed class QueryServiceRequestClientContacts : QueryEntitiesRequestBase<ClientContactDto>
    {
        public QueryServiceRequestClientContacts(int serviceRequestId)
            : base($"{ApiResources.ServiceRequests}/{serviceRequestId}/{ApiResources.Crm}")
        {
        }
    }
}

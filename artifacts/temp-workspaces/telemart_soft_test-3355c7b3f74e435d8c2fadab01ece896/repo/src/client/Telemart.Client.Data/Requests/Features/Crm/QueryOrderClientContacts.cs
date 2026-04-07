using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Crm
{
    public sealed class QueryOrderClientContacts : QueryEntitiesRequestBase<ClientContactDto>
    {
        public QueryOrderClientContacts(int orderId)
            : base($"{ApiResources.Orders}/{orderId}/{ApiResources.Crm}")
        {
        }
    }
}

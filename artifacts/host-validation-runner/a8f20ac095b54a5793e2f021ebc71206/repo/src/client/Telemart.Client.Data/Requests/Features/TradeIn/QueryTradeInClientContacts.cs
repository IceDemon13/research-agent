using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInClientContacts : QueryEntitiesRequestBase<ClientContactDto>
    {
        public QueryTradeInClientContacts(int tradeInId)
            : base($"{ApiResources.TradeIns}/{tradeInId}/{ApiResources.Crm}")
        {
        }
    }
}
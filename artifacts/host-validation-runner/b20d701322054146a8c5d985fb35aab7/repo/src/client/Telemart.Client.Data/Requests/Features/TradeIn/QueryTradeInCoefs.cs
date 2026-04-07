using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInCoefs : QueryEntitiesRequestBase<TradeInCoefDto>
    {
        public QueryTradeInCoefs()
            : base($"{ApiResources.TradeIns}/coefs")
        {
        }
    }
}
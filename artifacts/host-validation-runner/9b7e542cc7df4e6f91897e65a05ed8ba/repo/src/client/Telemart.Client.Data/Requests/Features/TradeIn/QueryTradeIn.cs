using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeIn : QueryEntityRequestBase<TradeInDto>
    {
        public QueryTradeIn(int id)
            : base(ApiResources.TradeIns, id)
        {
        }
    }
}
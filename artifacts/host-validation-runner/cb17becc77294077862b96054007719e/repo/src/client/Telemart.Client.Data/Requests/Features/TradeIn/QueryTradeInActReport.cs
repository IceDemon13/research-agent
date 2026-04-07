using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInActReport : QueryEntityRequestBase<Result<TradeInActReportDto>>
    {
        public QueryTradeInActReport(int id)
            : base(ApiResources.TradeIns, id, "act/report")
        {
        }
    }
}
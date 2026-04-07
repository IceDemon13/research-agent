using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser
{
    public sealed class QueryAllContractorPrices : CallActionWithBodyRequestBase<Dictionary<int, ParserContractorPriceDto[]>, ParserContractorPriceSearchDto>
    {
        public QueryAllContractorPrices(ParserContractorPriceSearchDto dto)
            : base(dto, ApiResources.Parser, "all_prices")
        {
        }
    }
}
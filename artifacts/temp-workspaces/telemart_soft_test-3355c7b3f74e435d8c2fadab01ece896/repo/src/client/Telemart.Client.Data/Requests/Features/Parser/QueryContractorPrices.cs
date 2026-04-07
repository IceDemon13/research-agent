using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser
{
    public sealed class QueryContractorPrices : CallActionWithBodyRequestBase<List<ParserContractorPriceDto>, ParserContractorPriceSearchDto>
    {
        public QueryContractorPrices(int? contractorId, int[] productIds)
            : base(new ParserContractorPriceSearchDto { ContractorId = contractorId, ProductIds = productIds }, "parser", "prices")
        {
        }
    }
}
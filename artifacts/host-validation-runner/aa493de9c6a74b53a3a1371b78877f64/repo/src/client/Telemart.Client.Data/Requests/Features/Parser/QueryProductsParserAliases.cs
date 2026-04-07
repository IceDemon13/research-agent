using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser
{
    public sealed class QueryProductsParserAliases : QueryEntitiesRequestBase<ParserAliasDto>
    {
        public QueryProductsParserAliases(IFilteringItem filter)
            : base(filter, $"{ApiResources.Parser}/products/aliases")
        {
        }
    }
}
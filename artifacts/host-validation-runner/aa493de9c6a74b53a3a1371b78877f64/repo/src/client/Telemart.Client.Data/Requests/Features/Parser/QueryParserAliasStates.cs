using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser
{
    public sealed class QueryParserAliasStates : QueryEntitiesRequestBase<ParserAliasStateDto>
    {
        private static readonly string StatesResource = $"{ApiResources.Parser}/states";

        public QueryParserAliasStates()
            : base(StatesResource)
        {
        }
    }
}
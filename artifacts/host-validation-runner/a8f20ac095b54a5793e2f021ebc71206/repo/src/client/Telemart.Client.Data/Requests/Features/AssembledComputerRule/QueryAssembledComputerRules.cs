using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public sealed class QueryAssembledComputerRules : QueryEntitiesRequestBase<AssembledComputerRuleDto>
    {
        public QueryAssembledComputerRules()
            : base(ApiResources.AssembledComputerRules)
        {
        }
    }
}
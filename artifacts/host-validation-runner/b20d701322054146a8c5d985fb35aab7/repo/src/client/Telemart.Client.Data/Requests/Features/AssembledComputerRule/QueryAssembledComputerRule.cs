using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public sealed class QueryAssembledComputerRule : QueryEntityRequestBase<AssembledComputerRuleDto>
    {
        public QueryAssembledComputerRule(int id)
            : base(ApiResources.AssembledComputerRules, id)
        {
        }
    }
}
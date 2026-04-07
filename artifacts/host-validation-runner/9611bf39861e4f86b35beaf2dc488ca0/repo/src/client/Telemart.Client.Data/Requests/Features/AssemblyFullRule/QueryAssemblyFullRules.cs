using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyFullRule;

namespace Telemart.Client.Data.Requests.Features.AssemblyFullRule
{
    public class QueryAssemblyFullRules : QueryEntitiesRequestBase<AssemblyFullRuleDto>
    {
        public QueryAssemblyFullRules()
            : base(ApiResources.AssemblyFull)
        {
        }
    }
}
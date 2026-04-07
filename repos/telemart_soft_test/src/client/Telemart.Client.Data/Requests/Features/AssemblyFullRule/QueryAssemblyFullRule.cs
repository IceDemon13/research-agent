using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyFullRule;

namespace Telemart.Client.Data.Requests.Features.AssemblyFullRule
{
    public class QueryAssemblyFullRule : QueryEntityRequestBase<AssemblyFullRuleDto>
    {
        public QueryAssemblyFullRule(int id)
            : base(ApiResources.AssemblyFull, id)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public class QueryFilteredAssembledComputerRules : QueryEntitiesRequestBase<AssembledComputerRuleDto>
    {
        public QueryFilteredAssembledComputerRules(IFilteringItem filter)
            : base(filter, ApiResources.AssembledComputerRules)
        {
        }
    }
}

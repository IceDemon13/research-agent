using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.CustomerBonus
{
    public class QueryBonusLogTypes : QueryEntitiesRequestBase<CustomerBonusLogTypeDto>
    {
        public QueryBonusLogTypes()
            : base($"{ApiResources.CustomerBonuses}/log_types")
        {
        }
    }
}
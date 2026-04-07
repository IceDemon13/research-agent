using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.BonusType
{
    public class QueryBonusTypes : QueryEntitiesRequestBase<BonusTypeDto>
    {
        public QueryBonusTypes()
            : base(ApiResources.BonusTypes)
        {
        }
    }
}
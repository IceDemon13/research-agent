using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.CustomerBonus
{
    public class QueryCustomerBonusLogs : QueryEntitiesRequestBase<CustomerBonusLogDto>
    {
        public QueryCustomerBonusLogs(int id)
            : base(ApiResources.Customers, id, "bonus_logs")
        {
        }
    }
}
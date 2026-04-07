using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.CustomerBonus
{
    public class QueryCustomerBonuses : QueryEntitiesRequestBase<CustomerBonusDto>
    {
        public QueryCustomerBonuses(int? customerId = null)
            : base(new CustomerBonusesFilteringItem(customerId), ApiResources.CustomerBonuses)
        {
        }

        public class CustomerBonusesFilteringItem : FilteringItemBase
        {
            public CustomerBonusesFilteringItem(int? customerId)
            {
                CustomerId = customerId;
            }

            [FilteringItemProperty("customerId")]
            public int? CustomerId { get; }
        }
    }
}

using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer
{
    public class QueryCustomers : QueryEntitiesPagedRequestBase<CustomerDto>
    {
        public QueryCustomers(IFilteringItem filter)
            : base(filter, ApiResources.Customers)
        {
        }
    }
}

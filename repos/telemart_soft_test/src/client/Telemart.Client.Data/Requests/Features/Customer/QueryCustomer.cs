using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer
{
    public class QueryCustomer : QueryEntityRequestBase<CustomerDto>
    {
        public QueryCustomer(int id)
            : base(ApiResources.Customers, id)
        {
        }
    }
}
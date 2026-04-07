using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer.Actions
{
    public class UnlockCustomer : UnlockRequestBase<CustomerDto>
    {
        public UnlockCustomer(int id, bool force = false)
            : base(force, ApiResources.Customers, id)
        {
        }
    }
}

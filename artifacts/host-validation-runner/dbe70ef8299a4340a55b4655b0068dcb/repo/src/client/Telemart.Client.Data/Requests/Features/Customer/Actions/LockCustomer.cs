using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer.Actions
{
    public class LockCustomer : LockRequestBase<CustomerDto>
    {
        public LockCustomer(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Customers, id)
        {
        }
    }
}

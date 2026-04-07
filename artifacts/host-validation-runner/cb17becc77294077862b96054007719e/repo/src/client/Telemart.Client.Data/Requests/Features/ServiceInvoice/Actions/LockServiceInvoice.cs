using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions
{
    public sealed class LockServiceInvoice : LockRequestBase<ServiceInvoiceDto>
    {
        public LockServiceInvoice(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ServiceInvoices, id)
        {
        }
    }
}

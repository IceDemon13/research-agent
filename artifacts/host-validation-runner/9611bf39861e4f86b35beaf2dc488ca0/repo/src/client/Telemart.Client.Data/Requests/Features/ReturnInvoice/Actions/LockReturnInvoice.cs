using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public class LockReturnInvoice : LockRequestBase<ReturnInvoiceDto>
    {
        public LockReturnInvoice(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ReturnInvoices, id)
        {
        }
    }
}

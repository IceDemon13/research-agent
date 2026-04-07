using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public class UnlockReturnInvoice : UnlockRequestBase<ReturnInvoiceDto>
    {
        public UnlockReturnInvoice(int id, bool force = false)
            : base(force, ApiResources.ReturnInvoices, id)
        {
        }
    }
}

using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions
{
    public sealed class UnlockServiceInvoice : UnlockRequestBase<ServiceInvoiceDto>
    {
        public UnlockServiceInvoice(int id, bool force = false)
            : base(force, ApiResources.ServiceInvoices, id)
        {
        }
    }
}

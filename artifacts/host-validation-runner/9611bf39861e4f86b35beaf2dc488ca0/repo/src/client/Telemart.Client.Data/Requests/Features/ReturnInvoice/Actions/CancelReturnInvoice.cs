using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public class CancelReturnInvoice : CallEntityActionRequestResultBase<ReturnInvoiceDto>
    {
        public CancelReturnInvoice(int id)
            : base(id, ApiResources.ReturnInvoices, "cancel")
        {
        }
    }
}

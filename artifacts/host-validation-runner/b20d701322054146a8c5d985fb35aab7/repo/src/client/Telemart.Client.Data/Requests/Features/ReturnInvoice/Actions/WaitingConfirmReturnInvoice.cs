using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public sealed class WaitingConfirmReturnInvoice : CallEntityActionWithBodyRequestResultBase<ReturnInvoiceDto, WaitingConfirmReturnInvoiceDto>
    {
        public WaitingConfirmReturnInvoice(int id, WaitingConfirmReturnInvoiceDto dto)
            : base(id, dto, ApiResources.ReturnInvoices, "waiting_confirm")
        {
        }
    }
}

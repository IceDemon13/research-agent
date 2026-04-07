using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public sealed class ConfirmReturnInvoice : CallEntityActionWithBodyRequestResultBase<ReturnInvoiceDto, ConfirmReturnInvoiceDto>
    {
        public ConfirmReturnInvoice(int id, ConfirmReturnInvoiceDto dto)
            : base(id, dto, ApiResources.ReturnInvoices, "confirm")
        {
        }
    }
}

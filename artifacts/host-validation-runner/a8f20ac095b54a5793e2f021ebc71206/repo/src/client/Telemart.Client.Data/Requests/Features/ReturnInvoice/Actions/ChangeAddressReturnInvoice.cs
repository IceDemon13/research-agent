using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public sealed class ChangeAddressReturnInvoice : CallEntityActionWithBodyRequestResultBase<ReturnInvoiceDto, ReturnInvoiceChangeAddressDto>
    {
        public ChangeAddressReturnInvoice(int id, ReturnInvoiceChangeAddressDto dto)
            : base(id, dto, ApiResources.ReturnInvoices, "change_address")
        {
        }
    }
}
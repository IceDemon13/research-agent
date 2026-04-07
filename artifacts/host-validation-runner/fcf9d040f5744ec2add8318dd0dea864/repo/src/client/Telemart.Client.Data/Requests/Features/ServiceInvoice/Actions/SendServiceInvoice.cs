using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions
{
    public sealed class SendServiceInvoice : CallEntityActionWithBodyRequestBase<ServiceInvoiceDto, SendServiceInvoiceDto>
    {
        public SendServiceInvoice(int id, SendServiceInvoiceDto dto)
            : base(id, dto, ApiResources.ServiceInvoices, "send")
        {
        }
    }
}
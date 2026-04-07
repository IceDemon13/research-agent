using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions
{
    public sealed class CompleteServiceInvoice : CallEntityActionWithBodyRequestResultBase<ServiceInvoiceDto, CompleteServiceInvoiceDto>
    {
        public CompleteServiceInvoice(int id, CompleteServiceInvoiceDto dto)
            : base(id, dto, ApiResources.ServiceInvoices, "complete")
        {
        }
    }
}
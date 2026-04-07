using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice
{
    public sealed class UpdateServiceInvoice : UpdateEntityRequestBase<Result<ServiceInvoiceDto>, ServiceInvoiceSaveDto>
    {
        public UpdateServiceInvoice(int serviceInvoiceId, ServiceInvoiceSaveDto dto)
            : base(dto, ApiResources.ServiceInvoices, serviceInvoiceId)
        {
        }
    }
}
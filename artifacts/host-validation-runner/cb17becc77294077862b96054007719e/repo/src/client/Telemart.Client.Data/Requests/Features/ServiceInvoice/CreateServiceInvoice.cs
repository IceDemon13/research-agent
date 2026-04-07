using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice
{
    public sealed class CreateServiceInvoice : CreateEntityRequestBase<Result<ServiceInvoiceDto>, ServiceInvoiceCreateDto>
    {
        public CreateServiceInvoice(ServiceInvoiceCreateDto dto)
            : base(dto, ApiResources.ServiceInvoices)
        {
        }
    }
}
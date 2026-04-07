using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class CreateReturnInvoiceDocument : CreateEntityResultRequestBase<ReturnInvoiceDocumentSimpleDto, ReturnInvoiceDocumentDto>
    {
        public CreateReturnInvoiceDocument(ReturnInvoiceDocumentDto dto)
            : base(dto, $"{ApiResources.ReturnInvoices}/{dto.ReturnInvoiceId}/documents")
        {
        }
    }
}

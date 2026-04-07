using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.FiscalDocument;
using Telemart.Fiscal.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.FiscalDocument
{
    public class CreateFiscalDocument : CreateEntityResultRequestBase<FiscalDocumentDto, FiscalDocumentCreateDto>
    {
        public CreateFiscalDocument(FiscalDocumentCreateDto dto)
            : base(dto, ApiResources.FiscalDocument)
        {
        }
    }
}
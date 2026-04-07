using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Fiscal.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.FiscalDocument
{
    public class CreateFiscalDocumentSellRequest : CreateEntityResultRequestBase<SellRequest, CreateFiscalDocumentSellDto>
    {
        public CreateFiscalDocumentSellRequest(CreateFiscalDocumentSellDto dto)
            : base(dto, $"{ApiResources.FiscalDocument}/create_sell_request")
        {
        }
    }
}
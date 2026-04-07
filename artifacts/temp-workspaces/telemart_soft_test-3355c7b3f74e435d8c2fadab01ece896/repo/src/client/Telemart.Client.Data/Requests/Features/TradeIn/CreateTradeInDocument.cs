using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class CreateTradeInDocument : CreateEntityResultRequestBase<TradeInDocumentDto, CreateTradeInDocumentDto>
    {
        public CreateTradeInDocument(int id, CreateTradeInDocumentDto createDto)
            : base(createDto, $"{ApiResources.TradeIns}/{id}/documents")
        {
        }
    }
}
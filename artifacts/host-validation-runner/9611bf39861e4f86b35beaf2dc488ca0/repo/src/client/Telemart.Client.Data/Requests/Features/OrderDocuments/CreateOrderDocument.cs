using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderDocuments
{
    public sealed class CreateOrderDocument : CreateEntityResultRequestBase<OrderDocumentDto, OrderCreateDocumentDto>
    {
        public CreateOrderDocument(int id, OrderCreateDocumentDto dto)
            : base(dto, $"{ApiResources.Orders}/{id}/documents")
        {
        }
    }
}
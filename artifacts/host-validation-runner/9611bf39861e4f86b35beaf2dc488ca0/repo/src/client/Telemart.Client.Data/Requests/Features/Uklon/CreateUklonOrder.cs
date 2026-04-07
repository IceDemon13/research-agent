using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Uklon
{
    public sealed class CreateUklonOrder : CreateEntityResultRequestBase<UklonDocumentDto, CreateUklonOrderDto>
    {
        public CreateUklonOrder(CreateUklonOrderDto dto)
            : base(dto, ApiResources.Uklon, "document")
        {
        }
    }
}
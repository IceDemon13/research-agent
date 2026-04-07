using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Gabaritka;

namespace Telemart.Client.Data.Requests.Features.Gabaritka
{
    public sealed class CreateGabaritkaTtnByOrder : CallActionWithBodyRequestResultBase<GabaritkaDocumentDto, GabaritkaDocumentCreateDto>
    {
        public CreateGabaritkaTtnByOrder(GabaritkaDocumentCreateDto dto)
            : base(dto, ApiResources.GabaritkaDocuments, "create_by_order")
        {
        }
    }
}
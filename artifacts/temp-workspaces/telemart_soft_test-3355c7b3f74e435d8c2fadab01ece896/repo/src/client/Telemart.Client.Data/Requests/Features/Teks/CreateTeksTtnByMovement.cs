using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Teks;

namespace Telemart.Client.Data.Requests.Features.Teks
{
    public sealed class CreateTeksTtnByMovement : CallActionWithBodyRequestResultBase<MovementDto, CreateTeksDocumentDto>
    {
        public CreateTeksTtnByMovement(CreateTeksDocumentDto dto)
            : base(dto, ApiResources.TeksDocuments, "create_by_movement")
        {
        }
    }
}
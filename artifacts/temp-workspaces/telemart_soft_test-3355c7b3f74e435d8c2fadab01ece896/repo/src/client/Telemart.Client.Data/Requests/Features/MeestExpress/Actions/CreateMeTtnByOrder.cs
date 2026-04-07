using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Data.Requests.Features.MeestExpress.Actions
{
    public sealed class CreateMeTtnByOrder : CallActionWithBodyRequestResultBase<MeDocumentDto, MeDocumentCreateDto>
    {
        public CreateMeTtnByOrder(MeDocumentCreateDto dto)
            : base(dto, ApiResources.MeestExpressDocuments, "create_by_order")
        {
        }
    }
}

using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaTtn;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class CreateNpTtn : CallActionWithBodyRequestResultBase<NpDocumentDto, NovaposhtaTtnCreateDto>
    {
        public CreateNpTtn(NovaposhtaTtnCreateDto dto)
            : base(dto, ApiResources.NovaposhtaDocuments, "create")
        {
        }
    }
}

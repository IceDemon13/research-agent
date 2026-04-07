using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaTtn;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class UpdateNpTtn : UpdateEntityResultRequestBase<NpDocumentDto, NpDocumentUpdateDto>
    {
        public UpdateNpTtn(string ttn, NpDocumentUpdateDto dto)
            : base(dto, ApiResources.NovaposhtaDocuments, ttn)
        {
        }
    }
}
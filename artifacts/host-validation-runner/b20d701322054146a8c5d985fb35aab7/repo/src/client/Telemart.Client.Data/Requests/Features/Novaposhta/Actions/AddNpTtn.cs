using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaTtn;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class AddNpTtn : CreateEntityResultRequestBase<NpDocumentDto, NpDocumentSaveDto>
    {
        public AddNpTtn(string ttn, string npContractorRef, int sourceId, int payerTypeId, string comment)
            : base(new NpDocumentSaveDto(ttn, npContractorRef, sourceId, payerTypeId, comment), ApiResources.NovaposhtaDocuments)
        {
        }
    }
}

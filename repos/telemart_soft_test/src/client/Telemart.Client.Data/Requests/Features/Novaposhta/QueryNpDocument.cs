using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpDocument : QueryEntityRequestBase<NpDocumentDto>
    {
        public QueryNpDocument(object number)
            : base(ApiResources.NovaposhtaDocuments, number)
        {
        }
    }
}
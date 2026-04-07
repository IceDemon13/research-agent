using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.NovaposhtaBill;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public class QueryNpBillDocument : QueryEntityRequestBase<NovaposhtaBillDocumentDto>
    {
        public QueryNpBillDocument(int id)
            : base(ApiResources.Novaposhta, "bill", id, "document")
        {
        }
    }
}
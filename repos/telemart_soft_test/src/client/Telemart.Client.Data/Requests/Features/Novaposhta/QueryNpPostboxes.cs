using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpPostboxes : QueryEntitiesRequestBase<NpWarehouseDto>
    {
        public QueryNpPostboxes(int cityId)
            : base(ApiResources.Novaposhta, "postboxes")
        {
            UrlParameters = new (string, object)[] { ("cityId", cityId) };
        }
    }
}
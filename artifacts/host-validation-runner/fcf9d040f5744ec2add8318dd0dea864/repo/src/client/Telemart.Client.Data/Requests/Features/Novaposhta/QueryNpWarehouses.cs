using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpWarehouses : QueryEntitiesRequestBase<NpWarehouseDto>
    {
        public QueryNpWarehouses(int cityId)
            : base(ApiResources.Novaposhta, "warehouses")
        {
            UrlParameters = new (string, object)[] { ("cityId", cityId) };
        }
    }
}
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Data.Requests.Features.MeestExpress
{
    public class QueryMeWarehouses : QueryEntitiesRequestBase<MeWarehouseDto>
    {
        public QueryMeWarehouses(int cityId)
            : base(ApiResources.MeestExpress, "warehouses")
        {
            UrlParameters = new (string Name, object Value)[] { ("cityId", cityId) };
        }
    }
}
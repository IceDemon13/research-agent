using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AutoSource;

namespace Telemart.Client.Data.Requests.Features.AutoSource
{
    public class QueryOrderProductSourceEntities : QueryEntitiesRequestBase<OrderProductSourceEntityDto>
    {
        public QueryOrderProductSourceEntities()
            : base(ApiResources.AutoSource, "order_product_sources")
        {
        }
    }
}
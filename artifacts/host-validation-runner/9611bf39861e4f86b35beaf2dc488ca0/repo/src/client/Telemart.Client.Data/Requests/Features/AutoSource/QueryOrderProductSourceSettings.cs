using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AutoSource;

namespace Telemart.Client.Data.Requests.Features.AutoSource
{
    public sealed class QueryOrderProductSourceSettings : QueryEntitiesRequestBase<OrderProductSourceSettingDto>
    {
        public QueryOrderProductSourceSettings()
            : base(ApiResources.AutoSource, "order_product_source_settings")
        {
        }
    }
}
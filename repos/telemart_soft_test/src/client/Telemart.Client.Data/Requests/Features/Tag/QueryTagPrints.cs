using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Tag
{
    public sealed class QueryTagPrints : QueryEntitiesPagedRequestBase<TagPrintInfoDto>
    {
        public QueryTagPrints(int? warehouseId)
            : base(new QueryTagPrintsFilteringItem(warehouseId), "tags/prints")
        {
        }

        private class QueryTagPrintsFilteringItem : FilteringItemBase
        {
            public QueryTagPrintsFilteringItem(int? warehouseId)
            {
                WarehouseId = warehouseId;
            }

            [FilteringItemProperty("warehouse")]
            public int? WarehouseId { get; set; }
        }
    }
}
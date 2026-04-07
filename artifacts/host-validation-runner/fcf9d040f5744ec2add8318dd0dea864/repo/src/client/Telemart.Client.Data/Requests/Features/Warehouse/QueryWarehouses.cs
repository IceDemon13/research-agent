using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class QueryWarehouses : QueryEntitiesPagedRequestBase<WarehouseDto>
    {
        public QueryWarehouses()
            : base(ApiResources.Warehouses)
        {
        }

        public QueryWarehouses(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.Warehouses)
        {
        }
    }
}
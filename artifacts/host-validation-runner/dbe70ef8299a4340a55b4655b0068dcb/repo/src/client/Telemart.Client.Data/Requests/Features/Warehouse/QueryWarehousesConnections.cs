using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class QueryWarehousesConnections : CallActionWithBodyRequestBase<WarehousesConnectionsResult, QueryWarehousesConnections.WarehouseConnectionsRequest>
    {
        public QueryWarehousesConnections(DateTime fromDate, int fromWarehouseId, int toWarehouseId, int maxTransitCount)
            : base(new WarehouseConnectionsRequest(fromDate, fromWarehouseId, toWarehouseId, maxTransitCount), ApiResources.Warehouses, "connections")
        {
        }

        public class WarehouseConnectionsRequest
        {
            public WarehouseConnectionsRequest(DateTime fromDate, int fromWarehouseId, int toWarehouseId, int maxTransitCount)
            {
                FromDate = fromDate;
                FromWarehouseId = fromWarehouseId;
                ToWarehouseId = toWarehouseId;
                MaxTransitCount = maxTransitCount;
            }

            [JsonProperty("from_date")]
            public DateTime FromDate { get; }

            [JsonProperty("from_warehouse_id")]
            public int FromWarehouseId { get; set; }

            [JsonProperty("to_warehouse_id")]
            public int ToWarehouseId { get; set; }

            [JsonProperty("max_transit_count")]
            public int MaxTransitCount { get; set; }
        }
    }
}

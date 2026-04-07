using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public class WarehousesConnectionsResult
    {
        [JsonProperty("connections")]
        public IReadOnlyCollection<IReadOnlyCollection<WarehouseConnectionDto>> Connections { get; set; }
    }
}
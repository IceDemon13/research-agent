using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public sealed record WarehouseAllowedEmployeesDto
    {
        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("employee_ids")]
        public IReadOnlyCollection<int> EmployeeIds { get; init; }
    }
}
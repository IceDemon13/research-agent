using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AssemblyService
{
    public sealed class AssemblyServiceSaveDto
    {
        public AssemblyServiceSaveDto(
            int id,
            int employeeId,
            IReadOnlyCollection<AssemblyServiceProductSaveDto> products)
        {
            Id = id;
            EmployeeId = employeeId;
            Products = products;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<AssemblyServiceProductSaveDto> Products { get; set; }
    }
}
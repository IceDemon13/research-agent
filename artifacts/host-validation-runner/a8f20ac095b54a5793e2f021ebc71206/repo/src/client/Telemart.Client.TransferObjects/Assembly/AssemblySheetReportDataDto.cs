using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Assembly
{
    public sealed record AssemblySheetReportDataDto
    {
        [JsonProperty("assembly_service_products")]
        public IReadOnlyCollection<OrderAssemblyProductReportDataDto> AssemblyServiceProducts { get; init; }

        [JsonProperty("assembly_id")]
        public int AssemblyId { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("name_assembly_computers")]
        public string NameAssemblyComputers { get; init; }

        [JsonProperty("name_assembly_employees")]
        public string NameAssemblyEmpolyees { get; init; }

        [JsonProperty("name_manager_employee")]
        public string NameManagerEmployee { get; init; }

        [JsonProperty("telegram_manager_employee")]
        public string TelegramManagerEmployee { get; init; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; init; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; init; }
    }
}
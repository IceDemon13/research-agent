using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderAssemblyReportDataDto
    {
        [JsonProperty("products")]
        public IReadOnlyCollection<OrderAssemblyProductReportDataDto> Products { get; init; }

        [JsonProperty("assembly_service_products")]
        public IReadOnlyCollection<OrderAssemblyProductReportDataDto> AssemblyServiceProducts { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("name_assembly_computers")]
        public string NameAssemblyComputers { get; init; }

        [JsonProperty("name_assembly_employees")]
        public string NameAssemblyEmpolyees { get; init; }

        [JsonProperty("name_confirm_employee")]
        public string NameConfirmEmployee { get; init; }

        [JsonProperty("telegram_confirm_employee")]
        public string TelegramConfirmEmployee { get; init; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; init; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; init; }
    }
}
using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.PackList;

namespace Telemart.Client.TransferObjects
{
    public class OrderAssemblyReportSimpleDataDto
    {
        [JsonProperty("products")]
        public IReadOnlyCollection<PackListPrintProductDto> Products { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("assembled_by_names")]
        public string AssembledByNames { get; set; }

        [JsonProperty("confirmed_by_name")]
        public string ConfirmedByName { get; set; }

        [JsonProperty("telegram_confirm_employee")]
        public string TelegramConfirmEmployee { get; set; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; set; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; set; }
    }
}

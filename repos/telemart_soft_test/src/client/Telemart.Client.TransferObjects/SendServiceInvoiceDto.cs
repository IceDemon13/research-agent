using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class SendServiceInvoiceDto
    {
        [JsonProperty("employee_carrier_id")]
        public int? EmployeeCarrierId { get; set; }

        [JsonProperty("ttn")]
        public string Ttn { get; set; }
    }
}
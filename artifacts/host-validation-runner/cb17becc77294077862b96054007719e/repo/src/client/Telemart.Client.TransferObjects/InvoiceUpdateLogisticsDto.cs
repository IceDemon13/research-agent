using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceUpdateLogisticsDto
    {
        public InvoiceUpdateLogisticsDto(
            int? employeeCarrierId,
            int carryId,
            string comment,
            IReadOnlyCollection<InvoiceTtnDto> trackNumbers)
        {
            EmployeeCarrierId = employeeCarrierId;
            CarryId = carryId;
            Comment = comment;
            TrackNumbers = trackNumbers;
        }

        [JsonProperty("employee_carrier_id")]
        public int? EmployeeCarrierId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("ttns")]
        public IReadOnlyCollection<InvoiceTtnDto> TrackNumbers { get; set; }
    }
}
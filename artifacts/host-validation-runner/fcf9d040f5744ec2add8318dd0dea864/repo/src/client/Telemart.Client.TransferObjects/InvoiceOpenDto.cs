using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceOpenDto
    {
        public InvoiceOpenDto(int invoiceId, DateTime dateClose, int employeeRequestId)
        {
            Id = invoiceId;
            DateClose = dateClose;
            EmployeeRequestId = employeeRequestId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date_close")]
        public DateTime DateClose { get; set; }

        [JsonProperty("employee_request_id")]
        public int EmployeeRequestId { get; set; }
    }
}
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Complaint
{
    public class ComplaintSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
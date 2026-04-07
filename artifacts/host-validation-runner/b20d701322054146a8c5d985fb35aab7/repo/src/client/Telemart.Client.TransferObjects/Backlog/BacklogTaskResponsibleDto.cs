using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public class BacklogTaskResponsibleDto
    {
        public BacklogTaskResponsibleDto(int id, int? responsibleId)
        {
            Id = id;
            EmployeeId = responsibleId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; set; }
    }
}
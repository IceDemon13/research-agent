using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceRepairSaveDto
    {
        public ServiceRepairSaveDto(int id, int? serviceCenterId, string defect, string comment)
        {
            Id = id;
            Defect = defect;
            Comment = comment;
            ServiceCenterId = serviceCenterId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("defect")]
        public string Defect { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("service_center_id")]
        public int? ServiceCenterId { get; set; }
    }
}
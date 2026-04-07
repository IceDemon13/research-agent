using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestDocumentSimpleSaveDto
    {
        public ServiceRequestDocumentSimpleSaveDto(int id, int typeId)
        {
            Id = id;
            TypeId = typeId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }
    }
}
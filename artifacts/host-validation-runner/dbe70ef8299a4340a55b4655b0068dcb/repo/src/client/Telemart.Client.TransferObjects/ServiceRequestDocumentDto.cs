using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestDocumentDto : ServiceRequestDocumentSimpleDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
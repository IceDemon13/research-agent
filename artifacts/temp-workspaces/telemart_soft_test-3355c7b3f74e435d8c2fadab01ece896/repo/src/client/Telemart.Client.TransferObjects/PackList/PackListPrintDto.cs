using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public class PackListPrintDto : PackListDto
    {
        [JsonProperty("products")]
        public PackListPrintProductDto[] Products { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }
    }
}
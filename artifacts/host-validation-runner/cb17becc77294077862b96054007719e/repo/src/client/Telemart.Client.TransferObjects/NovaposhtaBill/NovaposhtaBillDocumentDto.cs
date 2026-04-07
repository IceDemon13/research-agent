using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaBill
{
    public class NovaposhtaBillDocumentDto
    {
        [JsonProperty("bill_id")]
        public int BillId { get; set; }

        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductSnDto
    {
        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("warranty_removed")]
        public bool WarrantyRemoved { get; set; }
    }
}
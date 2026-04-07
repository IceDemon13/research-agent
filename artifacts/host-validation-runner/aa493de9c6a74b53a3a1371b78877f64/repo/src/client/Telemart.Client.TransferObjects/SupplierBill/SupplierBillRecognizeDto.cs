using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillRecognizeDto
    {
        [JsonProperty("supplier_name")]
        public string SupplierName { get; set; }

        [JsonProperty("supplier_code")]
        public string SupplierCode { get; set; }
    }
}
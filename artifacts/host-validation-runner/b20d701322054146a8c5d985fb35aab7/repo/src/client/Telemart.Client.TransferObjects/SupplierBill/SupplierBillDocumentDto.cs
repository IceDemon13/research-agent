using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillDocumentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("supplier_bill_id")]
        public int SupplierBillId { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }

        [JsonProperty("data")]
        public byte[] Data { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}
using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Customer
{
    public sealed class CustomerAssemblyDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_link")]
        public string ProductLink { get; set; }

        [JsonProperty("assembly_id")]
        public int AssemblyId { get; set; }

        [JsonProperty("customer_deleted")]
        public bool CustomerDeleted { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }
    }
}
using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SupplierWarehouseDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; init; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("pricer24_id")]
        public Guid? Pricer24Id { get; set; }

        [JsonProperty("supplier_warehouse_avails")]
        public string[] SupplierWarehouseAvails { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}
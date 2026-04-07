using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UpdateWarehouseNpAddressDto
    {
        public UpdateWarehouseNpAddressDto(int warehouseId, string house, string npStreetRef)
        {
            WarehouseId = warehouseId;
            House = house;
            NpStreetRef = npStreetRef;
        }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("np_street_ref")]
        public string NpStreetRef { get; set; }
    }
}
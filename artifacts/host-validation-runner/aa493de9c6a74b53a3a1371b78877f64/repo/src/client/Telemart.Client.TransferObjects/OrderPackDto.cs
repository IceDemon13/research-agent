using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderPackDto
    {
        public OrderPackDto(
            int id,
            int packagePlaces,
            decimal packageWeight,
            string packageTtn,
            OrderProductDeliveryDto[] orderProducts,
            TimeSpan? packTime,
            int[] cellIds)
        {
            Id = id;
            PackagePlaces = packagePlaces;
            PackageWeight = packageWeight;
            PackageTtn = packageTtn;
            OrderProducts = orderProducts;
            PackTime = packTime;
            CellIds = cellIds;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public decimal PackageWeight { get; set; }

        [JsonProperty("package_ttn")]
        public string PackageTtn { get; set; }

        [JsonProperty("products")]
        public OrderProductDeliveryDto[] OrderProducts { get; set; }

        [JsonProperty("pack_time")]
        public TimeSpan? PackTime { get; set; }

        [JsonProperty("cell_ids")]
        public int[] CellIds { get; set; }
    }
}
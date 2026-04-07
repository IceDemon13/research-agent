using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UkrposhtaTtnCreateByOrderDto
    {
        public UkrposhtaTtnCreateByOrderDto(
            int orderId,
            decimal weight,
            decimal lenght,
            bool fragile,
            IReadOnlyCollection<UpDocumentPackagePlaceDto> places)
        {
            OrderId = orderId;
            Places = places;
            Weight = weight;
            Length = lenght;
            Fragile = fragile;
        }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("weight")]
        public decimal Weight { get; set; }

        [JsonProperty("length")]
        public decimal Length { get; set; }

        [JsonProperty("fragile")]
        public bool Fragile { get; set; }

        [JsonProperty("places")]
        public IReadOnlyCollection<UpDocumentPackagePlaceDto> Places { get; set; }
    }
}
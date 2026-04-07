using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaTtn
{
    public class NovaposhtaTtnCreateByOrderDto
    {
        public NovaposhtaTtnCreateByOrderDto(
            int orderId,
            int packagePlaces,
            decimal packageWeight,
            int? width,
            int? length,
            int? height,
            bool addToApplication)
        {
            OrderId = orderId;
            PackagePlaces = packagePlaces;
            PackageWeight = packageWeight;
            Width = width;
            Length = length;
            Height = height;
            AddToApplication = addToApplication;
        }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public decimal PackageWeight { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("length")]
        public int? Length { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("add_to_application")]
        public bool AddToApplication { get; set; }
    }
}
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Teks
{
    public class TeksPackageDto
    {
        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public decimal PackageWeight { get; set; }

        [JsonProperty("volume_weight")]
        public decimal? VolumeWeight { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("length")]
        public int? Length { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("package_insurance")]
        public decimal PackageInsurance { get; set; }
    }
}
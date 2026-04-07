using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeDocumentCreateDto
    {
        public MeDocumentCreateDto(
            int orderId,
            int packagePlaces,
            decimal packageWeight,
            decimal packageInsurance,
            IReadOnlyCollection<MeDocumentPlaceCreateDto> places)
        {
            OrderId = orderId;
            PackagePlaces = packagePlaces;
            PackageWeight = packageWeight;
            PackageInsurance = packageInsurance;
            Places = places;
        }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public decimal PackageWeight { get; set; }

        [JsonProperty("package_insurance")]
        public decimal PackageInsurance { get; set; }

        [JsonProperty("places")]
        public IReadOnlyCollection<MeDocumentPlaceCreateDto> Places { get; set; }
    }
}
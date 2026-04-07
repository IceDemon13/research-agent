using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestCreateTrackNumberDto
    {
        public ServiceRequestCreateTrackNumberDto(
            int id,
            string recipient,
            string phone,
            int carryId,
            DeliveryDataDto deliveryData,
            int packagePlaces,
            double packageWeight,
            decimal packageInsurance,
            bool addToApplication,
            int? packageWidth,
            int? packageLength,
            int? packageHeight)
        {
            Id = id;
            Recipient = recipient;
            Phone = phone;
            CarryId = carryId;
            DeliveryData = deliveryData;
            PackagePlaces = packagePlaces;
            PackageWeight = packageWeight;
            PackageInsurance = packageInsurance;
            AddToApplication = addToApplication;
            Width = packageWidth;
            Length = packageLength;
            Height = packageHeight;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("recipient")]
        public string Recipient { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public double PackageWeight { get; set; }

        [JsonProperty("package_insurance")]
        public decimal PackageInsurance { get; set; }

        [JsonProperty("add_to_application")]
        public bool AddToApplication { get; set; }

        [JsonProperty("package_width")]
        public int? Width { get; set; }

        [JsonProperty("package_length")]
        public int? Length { get; set; }

        [JsonProperty("package_height")]
        public int? Height { get; set; }
    }
}
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaTtn
{
    public class NovaposhtaTtnCreateDto
    {
        [JsonProperty("np_contractor_ref")]
        public string NpContractorRef { get; set; }

        [JsonProperty("service_type")]
        public int ServiceType { get; set; }

        [JsonProperty("add_to_application")]
        public bool AddToApplication { get; set; }

        [JsonProperty("entity_type_id")]
        public int? EntityTypeId { get; set; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; set; }

        #region Sender

        [JsonProperty("sender_fio")]
        public string SenderFio { get; set; }

        [JsonProperty("sender_phone")]
        public string SenderPhone { get; set; }

        [JsonProperty("sender_warehouse_id")]
        public int SenderWarehouseId { get; set; }

        #endregion

        #region Recipient

        [JsonProperty("recipient_contractor_id")]
        public int RecipientContractorId { get; set; }

        [JsonProperty("recipient_last_name")]
        public string RecipientLastName { get; set; }

        [JsonProperty("recipient_first_name")]
        public string RecipientFistName { get; set; }

        [JsonProperty("recipient_middle_name")]
        public string RecipientMiddleName { get; set; }

        [JsonProperty("recipient_phone")]
        public string RecipientPhone { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("recipient_delivery_data")]
        public DeliveryDataDto RecipientDeliveryData { get; set; }

        #endregion

        #region Package

        [JsonProperty("source_id")]
        public int SourceId { get; set; }

        [JsonProperty("payer_type_id")]
        public int PayerTypeId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("package_insurance")]
        public decimal PackageInsurance { get; set; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public double PackageWeight { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        #endregion
    }
}
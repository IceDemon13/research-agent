using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Payments
{
    public class PaymentSaveDto
    {
        public PaymentSaveDto(
            int id,
            string name,
            string nameUa,
            string nameEn,
            decimal fee,
            decimal providerFee,
            int limitUah,
            int limitUsd,
            bool active)
        {
            Id = id;
            Name = name;
            NameUa = nameUa;
            NameEn = nameEn;
            Fee = fee;
            ProviderFee = providerFee;
            LimitUah = limitUah;
            LimitUsd = limitUsd;
            Active = active;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("provider_fee")]
        public decimal ProviderFee { get; set; }

        [JsonProperty("limit_uah")]
        public int LimitUah { get; set; }

        [JsonProperty("limit_usd")]
        public int LimitUsd { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}
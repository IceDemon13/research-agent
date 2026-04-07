using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    [DataContract]
    public sealed class ProductPriceSimpleDto
    {
        [DataMember(Order = 2)]
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty("price_type_id")]
        public int PriceTypeId { get; set; }

        [DataMember(Order = 4)]
        [JsonProperty("tag_color_id")]
        public int TagColorId { get; set; }

        [DataMember(Order = 5)]
        [JsonProperty("max_bonuses_to_use")]
        public int MaxBonusesToUse { get; set; }

        [DataMember(Order = 6)]
        [JsonProperty("partial_pay")]
        public int? PartialPay { get; set; }

        [DataMember(Order = 7)]
        [JsonProperty("price")]
        public decimal Price { get; set; }

        [DataMember(Order = 8)]
        [JsonProperty("price_prev")]
        public decimal? PricePrev { get; set; }

        [DataMember(Order = 9)]
        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [DataMember(Order = 10)]
        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [DataMember(Order = 11)]
        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [DataMember(Order = 12)]
        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [DataMember(Order = 13)]
        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [DataMember(Order = 14)]
        [JsonProperty("partial_pay_pb")]
        public int? PartialPayPb { get; set; }

        [DataMember(Order = 15)]
        [JsonProperty("partial_pay_pumb")]
        public int? PartialPayPumb { get; set; }

        [DataMember(Order = 16)]
        [JsonProperty("partial_pay_ab")]
        public int? PartialPayAb { get; set; }
    }
}
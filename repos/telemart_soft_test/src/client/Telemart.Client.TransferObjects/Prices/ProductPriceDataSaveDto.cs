using System.Runtime.Serialization;
using Newtonsoft.Json;
using Telemart.PriceCalculation.Context.Dictionaries;

namespace Telemart.Client.TransferObjects.Prices
{
    public class ProductPriceDataSaveDto
    {
        public ProductPriceDataSaveDto(
            int priceTypeId,
            decimal price,
            decimal? pricePrev,
            int maxBonusesToUse,
            int tagColorId,
            int? partialPay,
            int? partialPayPb,
            int? partialPayPumb,
            int? partialPayAb)
        {
            PriceTypeId = priceTypeId;
            Price = price;
            PricePrev = pricePrev;
            MaxBonusesToUse = maxBonusesToUse;
            TagColorId = tagColorId;
            PartialPay = partialPay;
            PartialPayPb = partialPayPb;
            PartialPayPumb = partialPayPumb;
            PartialPayAb = partialPayAb;
        }

        [JsonProperty("price_type_id")]
        public int PriceTypeId { get; set; }

        [JsonProperty("tag_color_id")]
        public int? TagColorId { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("price_prev")]
        public decimal? PricePrev { get; set; }

        [JsonProperty("max_bonuses_to_use")]
        public int MaxBonusesToUse { get; set; }

        [JsonProperty("partial_pay")]
        public int? PartialPay { get; set; }

        [JsonProperty("partial_pay_pb")]
        public int? PartialPayPb { get; set; }

        [JsonProperty("partial_pay_pumb")]
        public int? PartialPayPumb { get; set; }

        [JsonProperty("partial_pay_ab")]
        public int? PartialPayAb { get; set; }

        [JsonProperty("price_politic_name")]
        public string PricePoliticName { get; set; }
    }
}
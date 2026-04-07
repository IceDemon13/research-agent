using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Telemart.PriceCalculation.Context;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public class ProductPriceSaveDto
    {
        public ProductPriceSaveDto(
            int productId,
            int availId,
            int? availModifiedBy,
            int? labelRetailId,
            int? labelWholesaleId,
            int robotModeManualId,
            int robotModeAutoId,
            string priceComment,
            int hotline,
            int ratio,
            int? minLeftover,
            int rozetka,
            int monomarket,
            bool freeDelivery,
            bool showInAccessories,
            int plannedLeftover,
            int? bonusTypeId,
            int? bonusesToCharge,
            double? maxTradeInPrice,
            bool mining,
            int? showcasePickupModeId,
            int reserveQuantity,
            ProductPriceDataSaveDto[] prices)
        {
            Id = productId;
            AvailId = availId;
            AvailModifiedBy = availModifiedBy;
            LabelRetailId = labelRetailId;
            LabelWholesaleId = labelWholesaleId;
            RobotModeManualId = robotModeManualId;
            RobotModeAutoId = robotModeAutoId;
            PriceComment = priceComment;
            Hotline = hotline;
            Ratio = ratio;
            MinLeftover = minLeftover;
            Rozetka = rozetka;
            Monomarket = monomarket;
            FreeDelivery = freeDelivery;
            ShowInAccessories = showInAccessories;
            PlannedLeftover = plannedLeftover;
            Prices = prices;
            BonusTypeId = bonusTypeId;
            BonusesToCharge = bonusesToCharge;

            MaxTradeInPrice = maxTradeInPrice;

            Mining = mining;
            ShowcasePickupModeId = showcasePickupModeId;
            IgnoreOnReturn = false;
            ReservedQuantity = reserveQuantity;
        }

        [DataMember(Order = 1)]
        [JsonProperty("id")]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("avail_id")]
        public int AvailId { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty("avail_modified_by")]
        public int? AvailModifiedBy { get; set; }

        [DataMember(Order = 4)]
        [JsonProperty("robot_mode_manual_id")]
        public int RobotModeManualId { get; set; }

        [DataMember(Order = 5)]
        [JsonProperty("robot_mode_auto_id")]
        public int RobotModeAutoId { get; set; }

        [DataMember(Order = 6)]
        [JsonProperty("label_retail_id")]
        public int? LabelRetailId { get; set; }

        [DataMember(Order = 7)]
        [JsonProperty("label_wholesale_id")]
        public int? LabelWholesaleId { get; set; }

        [DataMember(Order = 8)]
        [JsonProperty("price_comment")]
        public string PriceComment { get; set; }

        [DataMember(Order = 9)]
        [JsonProperty("hotline")]
        public int Hotline { get; set; }

        [DataMember(Order = 10)]
        [JsonProperty("ratio")]
        public int Ratio { get; set; }

        [DataMember(Order = 11)]
        [JsonProperty("min_leftover")]
        public int? MinLeftover { get; set; }

        [DataMember(Order = 12)]
        [JsonProperty("rozetka")]
        public int Rozetka { get; set; }

        [DataMember(Order = 13)]
        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }

        [DataMember(Order = 14)]
        [JsonProperty("show_in_accessories")]
        public bool ShowInAccessories { get; set; }

        [DataMember(Order = 15)]
        [JsonProperty("planned_leftover")]
        public int PlannedLeftover { get; set; }

        [DataMember(Order = 16)]
        [JsonProperty("bonus_type_id")]
        public int? BonusTypeId { get; set; }

        [DataMember(Order = 17)]
        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }

        [DataMember(Order = 18)]
        [JsonProperty("max_trade_in_price")]
        public double? MaxTradeInPrice { get; set; }

        [DataMember(Order = 19)]
        [JsonProperty("mining")]
        public bool Mining { get; set; }

        [DataMember(Order = 20)]
        [JsonProperty("ignore_on_return")]
        public bool IgnoreOnReturn { get; set; }

        [DataMember(Order = 21)]
        [JsonProperty("prices")]
        public ProductPriceDataSaveDto[] Prices { get; set; }

        [DataMember(Order = 22)]
        [JsonProperty("showcase_pickup_mode_id")]
        public int? ShowcasePickupModeId { get; set; }

        [DataMember(Order = 23)]
        [JsonProperty("reserved_quantity")]
        public int? ReservedQuantity { get; set; }

        [DataMember(Order = 24)]
        [JsonProperty("property_changes")]
        public IReadOnlyCollection<PropertyChangeDto> PropertyChanges { get; set; }

        [DataMember(Order = 25)]
        [JsonProperty("monomarket")]
        public int Monomarket { get; set; }
    }
}
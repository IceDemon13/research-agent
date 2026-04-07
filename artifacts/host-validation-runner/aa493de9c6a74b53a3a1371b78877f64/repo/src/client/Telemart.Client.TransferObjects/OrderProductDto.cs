using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Promo;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductDto : OrderProductSimpleDto
    {
        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("additional_warranty")]
        public bool AdditionalWarranty { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("order_promo_code_id")]
        public int? OrderPromoCodeId { get; set; }

        [JsonProperty("invoice_id")]
        public int? InvoiceId { get; set; }

        [JsonProperty("movement_id")]
        public int? MovementId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("price_id")]
        public int? PriceId { get; set; }

        [JsonProperty("price_1c")]
        public decimal Price1C { get; set; }

        [JsonProperty("order_folder_id")]
        public int? OrderFolderId { get; set; }

        [JsonProperty("product")]
        public ProductSimpleDto Product { get; set; }

        [JsonProperty("unavailable_product_id")]
        public int? UnavailableProductId { get; set; }

        [JsonProperty("unavailable_order_product_price")]
        public decimal? UnavailableOrderProductPrice { get; set; }

        [JsonProperty("unavailable_product")]
        public ProductSimpleDto UnavailableProduct { get; set; }

        [JsonProperty("promo")]
        public CatalogPromoSimpleDto Promo { get; set; }

        [JsonProperty("promo_discount")]
        public decimal PromoDiscount { get; set; }

        [JsonProperty("serial_numbers")]
        public List<OrderProductSnDto> SerialNumbers { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("warranty_id")]
        public int WarrantyId { get; set; }

        [JsonProperty("label_id")]
        public int? LabelId { get; set; }

        [JsonProperty("assembly_quantity")]
        public int? AssemblyQuantity { get; set; }

        [JsonProperty("assembly_id")]
        public int? AssemblyId { get; set; }

        [JsonProperty("assembly_included")]
        public bool AssemblyIncluded { get; set; }

        [JsonProperty("bonus_type_id")]
        public int? BonusTypeId { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }

        [JsonProperty("bonuses_charged")]
        public bool BonusesCharged { get; set; }

        [JsonProperty("max_bonuses_to_use")]
        public int? MaxBonusesToUse { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }
    }
}
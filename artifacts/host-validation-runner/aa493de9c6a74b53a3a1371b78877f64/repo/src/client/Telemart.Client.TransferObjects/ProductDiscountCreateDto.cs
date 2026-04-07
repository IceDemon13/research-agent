using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductDiscountCreateDto
    {
        [JsonProperty("service_product_type_id")]
        public int ServiceProductTypeId { get; init; }

        [JsonProperty("defect_id")]
        public int? DefectId { get; init; }

        [JsonProperty("defect")]
        public string Defect { get; init; }

        [JsonProperty("defect_ukr")]
        public string DefectUkr { get; init; }

        [JsonProperty("defect_en")]
        public string DefectEn { get; init; }

        [JsonProperty("defect_description")]
        public string DefectDescription { get; init; }

        [JsonProperty("defect_description_ukr")]
        public string DefectDescriptionUkr { get; init; }

        [JsonProperty("defect_description_en")]
        public string DefectDescriptionEn { get; init; }

        [JsonProperty("warranty_wholesale_id")]
        public int WarrantyWholesaleId { get; init; }

        [JsonProperty("warranty_retail_id")]
        public int WarrantyRetailId { get; init; }

        [JsonProperty("images")]
        public IReadOnlyCollection<ImageDto> Images { get; init; }

        [JsonProperty("use_new_product_photo")]
        public bool UseNewProductPhoto { get; init; }

        [JsonProperty("discount_product_prefix_id")]
        public int DiscountProductPrefixId { get; init; }

        [JsonProperty("defect_create_discount")]
        public bool DefectCreateDiscount { get; init; }

        [JsonProperty("bed_pixel_quantity")]
        public int? BadPixelQuantity { get; init; }
    }
}
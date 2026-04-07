using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public class ProductAttributesDto : ILocalіzableEntity
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name_ua")]
        public string FullNameUa { get; set; }

        [JsonProperty("full_name_en")]
        public string FullNameEn { get; set; }

        [JsonProperty("parent_category_name")]
        public string ParentCategoryName { get; set; }

        [JsonProperty("parent_category_name_ukr")]
        public string ParentCategoryNameUkr { get; init; }

        [JsonProperty("parent_category_name_en")]
        public string ParentCategoryNameEn { get; init; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("self_barcode")]
        public bool SelfBarcode { get; set; }

        [JsonProperty("print_wcard")]
        public bool PrintWarrantyCard { get; set; }

        [JsonProperty("warranty_retail")]
        public string WarrantyRetail { get; set; }

        [JsonProperty("warranty_retail_id")]
        public int WarrantyRetailId { get; set; }

        [JsonProperty("warranty_wholesale")]
        public string WarrantyWholesale { get; set; }

        [JsonProperty("warranty_wholesale_id")]
        public int WarrantyWholesaleId { get; set; }

        [JsonProperty("product_serial_number_length")]
        public List<ProductSnLengthDto> SerialNumberLength { get; set; }

        [JsonProperty("keep_dimensions")]
        public bool KeepDimensions { get; set; }

        [JsonProperty("width")]
        public double? Width { get; set; }

        [JsonProperty("height")]
        public double? Height { get; set; }

        [JsonProperty("depth")]
        public double? Depth { get; set; }

        [JsonProperty("weight")]
        public double? Weight { get; set; }

        [JsonProperty("sticker_fragile")]
        public bool? StickerFragile { get; set; }

        [JsonProperty("sticker_this_way_up")]
        public bool? StickerThisWayUp { get; set; }

        [JsonProperty("barcodes")]
        public List<ProductBarcodeDto> Barcodes { get; set; }

        [JsonProperty("competitors_barcodes")]
        public List<string> CompetitorsBarcodes { get; set; }

        [JsonProperty("serials")]
        public List<string> Serials { get; set; }

        [JsonProperty("any_images")]
        public bool AnyImages { get; init; }

        [JsonProperty("part_number")]
        public string PartNumber { get; init; }

        [JsonProperty("nomenclature_series_accounting")]
        public bool NomenclatureSeriesAccounting { get; init; }

        string ILocalіzableEntity.Name => FullName;

        public string NameUkr => FullNameUa;

        public string NameEn => FullNameEn;

        public bool DimmensionsIsValid()
        {
            return !KeepDimensions ||
                   (Width.HasValue &&
                    Height.HasValue &&
                    Depth.HasValue &&
                    Weight.HasValue);
        }
    }
}
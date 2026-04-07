using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public class ProductCardDto : ILocalіzableEntity
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("category")]
        public CategoryDto Category { get; set; }

        [JsonProperty("product_day_category_id")]
        public int? ProductDayCategoryId { get; set; }

        [JsonProperty("product_day_position")]
        public int? ProductDayPosition { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("name_full_ua")]
        public string NameFullUa { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("self_barcode")]
        public bool SelfBarcode { get; set; }

        [JsonProperty("width")]
        public double? Width { get; set; }

        [JsonProperty("height")]
        public double? Height { get; set; }

        [JsonProperty("depth")]
        public double? Depth { get; set; }

        [JsonProperty("weight")]
        public double? Weight { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("image_links")]
        public string[] ImageLinks { get; set; }

        [JsonProperty("barcodes")]
        public ICollection<ProductBarcodeDto> Barcodes { get; set; }

        [JsonProperty("serial_number_length")]
        public ICollection<ProductSnLengthDto> SerialNumberLength { get; set; }
    }
}
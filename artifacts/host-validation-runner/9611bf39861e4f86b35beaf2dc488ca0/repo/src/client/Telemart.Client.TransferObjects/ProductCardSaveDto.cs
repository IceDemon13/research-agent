using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductCardSaveDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("product_day_category_id")]
        public int? ProductDayCategoryId { get; init; }

        [JsonProperty("product_day_position")]
        public int? ProductDayPosition { get; init; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; init; }

        [JsonProperty("self_barcode")]
        public bool SelfBarcode { get; init; }

        [JsonProperty("width")]
        public double? Width { get; init; }

        [JsonProperty("height")]
        public double? Height { get; init; }

        [JsonProperty("depth")]
        public double? Depth { get; init; }

        [JsonProperty("weight")]
        public double? Weight { get; init; }

        [JsonProperty("active")]
        public double Active { get; init; }

        [JsonProperty("serial_number_length")]
        public ICollection<ProductSnLengthDto> SerialNumberLength { get; init; }

        [JsonProperty("active_barcode_ids")]
        public int[] ActiveBarcodeIds { get; init; }

        [JsonProperty("fiscal_registrar_barcode_ids")]
        public int[] FiscalRegistrarBarcodeIds { get; init; }
    }
}
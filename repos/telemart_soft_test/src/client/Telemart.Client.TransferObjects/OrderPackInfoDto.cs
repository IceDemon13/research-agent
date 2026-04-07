using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderPackInfoDto
    {
        [JsonProperty("order")]
        public OrderDto Order { get; set; }

        [JsonProperty("insurance")]
        public decimal Insurance { get; set; }

        [JsonProperty("money_back")]
        public decimal MoneyBack { get; set; }

        [JsonProperty("bill_id")]
        public int? BillId { get; set; }

        [JsonProperty("cell_ids")]
        public int[] CellIds { get; set; }

        [JsonProperty("products_attributes")]
        public IReadOnlyCollection<ProductAttributesDto> ProductsAttributes { get; set; }

        [JsonIgnore]
        public string ScannedBarcode { get; set; }
    }
}
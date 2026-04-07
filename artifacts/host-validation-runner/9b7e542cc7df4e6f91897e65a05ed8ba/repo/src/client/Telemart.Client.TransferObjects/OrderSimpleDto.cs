using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderSimpleDto : OrderBaseDto
    {
        [JsonProperty("products")]
        public IReadOnlyCollection<OrderProductSimpleDto> Products { get; set; }

        [JsonProperty("preorder")]
        public bool Preorder { get; init; }

        [JsonProperty("fiscal_id")]
        public string FiscalId { get; init; }

        [JsonProperty("canceled_from_site")]
        public bool CanceledFromSite { get; init; }

        [JsonProperty("money_back_amount")]
        public decimal? MoneyBackAmount { get; init; }

        [JsonProperty("external_order_id")]
        public string ExternalOrderId { get; init; }

        [JsonProperty("source_id")]
        public int? OrderSourceId { get; init; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }
    }
}
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Gabaritka
{
    public class GabaritkaTrackNumberDto
    {
        [JsonProperty("order_status")]
        public string OrderStatus { get; set; }

        [JsonProperty("order_status_name")]
        public string OrderStatusName { get; set; }

        [JsonProperty("ordernumber")]
        public string OrderNumber { get; set; }

        [JsonProperty("weight")]
        public string Weight { get; set; }

        [JsonProperty("pieces")]
        public string Pieces { get; set; }

        [JsonProperty("date_from")]
        public string DateFrom { get; set; }

        [JsonProperty("time1_from")]
        public string DateFrom1 { get; set; }

        [JsonProperty("time2_from")]
        public string DateFrom2 { get; set; }

        [JsonProperty("date_to")]
        public string DateTo { get; set; }

        [JsonProperty("time1_to")]
        public string DateTo1 { get; set; }

        [JsonProperty("time2_to")]
        public string DateTo2 { get; set; }

        [JsonProperty("COD_amount")]
        public string CodAmount { get; set; }

        [JsonProperty("order_sum_from_contract")]
        public string OrderSumFromContract { get; set; }
    }
}
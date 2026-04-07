using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public class ProductSalesDto
    {
        [DataMember(Order = 1)]
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("orders_count")]
        public int OrdersCount { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty("products_count")]
        public int ProductsCount { get; set; }

        [DataMember(Order = 4)]
        [JsonProperty("avg_price_usd")]
        public double AvgPriceUsd { get; set; }
    }
}
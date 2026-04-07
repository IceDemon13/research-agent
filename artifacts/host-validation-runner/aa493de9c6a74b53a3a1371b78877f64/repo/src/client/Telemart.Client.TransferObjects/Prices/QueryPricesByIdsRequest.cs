using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public sealed class QueryPricesByIdsRequest
    {
        public QueryPricesByIdsRequest(IReadOnlyCollection<int> productIds, IReadOnlyCollection<int> contractorIds)
        {
            ProductIds = productIds ?? throw new ArgumentNullException(nameof(productIds));
            ContractorIds = contractorIds ?? Array.Empty<int>();
        }

        public QueryPricesByIdsRequest()
        {
        }

        [DataMember(Order = 1)]
        [JsonProperty("product_ids")]
        public IReadOnlyCollection<int> ProductIds { get; set; }

        [DataMember(Order = 1)]
        [JsonProperty("contractor_ids")]
        public IReadOnlyCollection<int> ContractorIds { get; set; }
    }
}
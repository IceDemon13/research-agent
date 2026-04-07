using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class QuerySupplierProductsDto
    {
        public QuerySupplierProductsDto(IReadOnlyCollection<int> productIds, IReadOnlyCollection<int> supplierIds)
        {
            ProductIds = productIds;
            SupplierIds = supplierIds;
        }

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<int> ProductIds { get; set; }

        [JsonProperty("supplier_ids")]
        public IReadOnlyCollection<int> SupplierIds { get; set; }
    }
}
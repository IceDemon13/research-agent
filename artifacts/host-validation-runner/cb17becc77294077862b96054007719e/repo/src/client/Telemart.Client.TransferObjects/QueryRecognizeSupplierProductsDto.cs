using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class QueryRecognizeSupplierProductsDto
    {
        public QueryRecognizeSupplierProductsDto(List<RecognizeSupplierProductDto> recognizeProducts, int supplierId)
        {
            SupplierId = supplierId;
            RecognizeProducts = recognizeProducts;
        }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; }

        [JsonProperty("recognize_products")]
        public List<RecognizeSupplierProductDto> RecognizeProducts { get; }
    }
}
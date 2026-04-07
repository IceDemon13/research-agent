using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public class CheckProductCompatibility : CallActionWithBodyRequestBase<CheckCompatibilityResponse, CheckProductCompatibility.CheckCompatibilityRequest>
    {
        public CheckProductCompatibility(IReadOnlyCollection<ProductQuantityDto> products, int? assemblyComplectationProductId)
            : base(new CheckCompatibilityRequest(products, assemblyComplectationProductId), "products", "check_compatibility")
        {
        }

        public class CheckCompatibilityRequest
        {
            public CheckCompatibilityRequest(IReadOnlyCollection<ProductQuantityDto> products, int? assemblyComplectationProductId)
            {
                Products = products;
                AssemblyComplectationProductId = assemblyComplectationProductId;
            }

            [JsonProperty("products")]
            public IReadOnlyCollection<ProductQuantityDto> Products { get; set; }

            [JsonProperty("assembly_complectation_product_id")]
            public int? AssemblyComplectationProductId { get; set; }
        }
    }
}
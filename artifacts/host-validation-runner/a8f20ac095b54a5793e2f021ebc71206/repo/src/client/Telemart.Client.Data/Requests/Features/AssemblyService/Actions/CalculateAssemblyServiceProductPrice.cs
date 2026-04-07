using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public class CalculateAssemblyServiceProductPrice : CallActionWithBodyRequestResultBase<CalculateAssemblyServiceProductPriceResponse, CalculateAssemblyServiceProductPriceRequest>
    {
        public CalculateAssemblyServiceProductPrice(decimal assemblyPrice)
            : base(new CalculateAssemblyServiceProductPriceRequest(assemblyPrice), ApiResources.AssemblyService, "calculate_service_price")
        {
        }
    }

    public class CalculateAssemblyServiceProductPriceRequest
    {
        public CalculateAssemblyServiceProductPriceRequest(decimal assemblyPrice)
        {
            AssemblyPrice = assemblyPrice;
        }

        [JsonProperty("assembly_price")]
        public decimal AssemblyPrice { get; set; }
    }

    public class CalculateAssemblyServiceProductPriceResponse
    {
        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}
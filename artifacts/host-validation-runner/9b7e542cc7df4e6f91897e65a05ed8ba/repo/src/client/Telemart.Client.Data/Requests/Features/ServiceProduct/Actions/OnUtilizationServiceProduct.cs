using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class OnUtilizationServiceProduct : CallEntityActionWithBodyRequestResultBase<ServiceProductDto, OnUtilizationServiceProduct.ServiceProductUtilizeDto>
    {
        public OnUtilizationServiceProduct(int serviceProductId, string description)
            : base(serviceProductId, new ServiceProductUtilizeDto(serviceProductId, description), ApiResources.ServiceProducts, "on_utilization")
        {
        }

        public class ServiceProductUtilizeDto
        {
            public ServiceProductUtilizeDto(int id, string description)
            {
                Id = id;
                Description = description;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }
        }
    }
}
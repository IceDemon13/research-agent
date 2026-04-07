using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public class ChangeProductServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ChangeProductServiceRequest.ChangeProductServiceRequestDto>
    {
        public ChangeProductServiceRequest(int serviceRequestId, int productId)
            : base(serviceRequestId, new ChangeProductServiceRequestDto(serviceRequestId, productId), ApiResources.ServiceRequests, "change-product")
        {
        }

        public class ChangeProductServiceRequestDto
        {
            public ChangeProductServiceRequestDto(int serviceRequestId, int productId)
            {
                ServiceRequestId = serviceRequestId;
                ProductId = productId;
            }

            [JsonProperty("service_request_id")]
            public int ServiceRequestId { get; set; }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }
        }
    }
}

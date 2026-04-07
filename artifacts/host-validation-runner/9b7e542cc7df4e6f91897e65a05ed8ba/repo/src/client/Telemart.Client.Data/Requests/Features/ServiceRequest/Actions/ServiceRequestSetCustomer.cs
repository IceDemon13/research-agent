using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public class ServiceRequestSetCustomer : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ServiceRequestSetCustomer.ServiceRequestSetCustomerDto>
    {
        public ServiceRequestSetCustomer(int orderId, int customerId)
            : base(orderId, new ServiceRequestSetCustomerDto(orderId, customerId), ApiResources.ServiceRequests, "set_customer")
        {
        }

        public class ServiceRequestSetCustomerDto
        {
            public ServiceRequestSetCustomerDto(int serviceRequestId, int customerId)
            {
                ServiceRequestId = serviceRequestId;
                CustomerId = customerId;
            }

            [JsonProperty("service_request_id")]
            public int ServiceRequestId { get; set; }

            [JsonProperty("customer_id")]
            public int CustomerId { get; set; }
        }
    }
}

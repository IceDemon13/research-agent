using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class ConfirmRepairServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ConfirmRepairServiceRequest.ServiceRequestConfirmRepairDto>
    {
        public ConfirmRepairServiceRequest(int serviceRequestId, int? serviceCenterId, AssembledComputerSaveDto assembledComputerSaveDto)
            : base(
                serviceRequestId,
                new ServiceRequestConfirmRepairDto
                {
                    Id = serviceRequestId,
                    ServiceCenterId = serviceCenterId,
                    AssembledComputerSaveDto = assembledComputerSaveDto
                },
                ApiResources.ServiceRequests,
                "repair")
        {
        }

        public class ServiceRequestConfirmRepairDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("service_center_id")]
            public int? ServiceCenterId { get; set; }

            [JsonProperty("assembled_computer_save_dto")]
            public AssembledComputerSaveDto AssembledComputerSaveDto { get; init; }
        }
    }
}
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderContractor : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderContractor.UpdateOrderContractorDto>
    {
        public UpdateOrderContractor(int orderId, int contractorId, bool? force = null)
            : base(orderId, new UpdateOrderContractorDto { ContractorId = contractorId, Force = force }, ApiResources.Orders, "set_contractor")
        {
        }

        public sealed class UpdateOrderContractorDto
        {
            [JsonProperty("contractor_id")]
            public int ContractorId { get; set; }

            [JsonProperty("force")]
            public bool? Force { get; set; }
        }
    }
}
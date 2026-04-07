using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public class ConfirmServiceRequestOnFiscalRegistrar : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ConfirmServiceRequestOnFiscalRegistrarDto>
    {
        public ConfirmServiceRequestOnFiscalRegistrar(int id, string fiscalId, bool completedOnFiscalRegistrar)
            : base(id, new ConfirmServiceRequestOnFiscalRegistrarDto(id, fiscalId, completedOnFiscalRegistrar), ApiResources.ServiceRequests, "confirm_on_fiscal_registrar")
        {
        }
    }

    public class ConfirmServiceRequestOnFiscalRegistrarDto
    {
        public ConfirmServiceRequestOnFiscalRegistrarDto(int serviceRequestId, string fiscalId, bool completedOnFiscalRegistrar)
        {
            ServiceRequestId = serviceRequestId;
            FiscalId = fiscalId;
            CompletedOnFiscalRegistrar = completedOnFiscalRegistrar;
        }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; init; }

        [JsonProperty("fiscal_id")]
        public string FiscalId { get; init; }

        [JsonProperty("completed_on_fiscal_registrar")]
        public bool CompletedOnFiscalRegistrar { get; init; }
    }
}
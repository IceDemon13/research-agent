using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public class ConfirmRefundOnFiscalRegistrar : CallEntityActionWithBodyRequestResultBase<RefundDto, ConfirmRefundOnFiscalRegistrar.ConfirmRefundOnFiscalRegistrarDto>
    {
        public ConfirmRefundOnFiscalRegistrar(int id, string fiscalId, bool completedOnFiscalRegistrar)
            : base(id, new ConfirmRefundOnFiscalRegistrarDto(id, fiscalId, completedOnFiscalRegistrar), ApiResources.Refunds, "confirm_on_fiscal_registrar")
        {
        }

        public class ConfirmRefundOnFiscalRegistrarDto
        {
            public ConfirmRefundOnFiscalRegistrarDto(int refundId, string fiscalId, bool completedOnFiscalRegistrar)
            {
                RefundId = refundId;
                FiscalId = fiscalId;
                CompletedOnFiscalRegistrar = completedOnFiscalRegistrar;
            }

            [JsonProperty("refund_id")]
            public int RefundId { get; set; }

            [JsonProperty("fiscal_id")]
            public string FiscalId { get; set; }

            [JsonProperty("completed_on_fiscal_registrar")]
            public bool CompletedOnFiscalRegistrar { get; set; }
        }
    }
}
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class ConfirmOrderOnFiscalRegistrar : CallEntityActionWithBodyRequestResultBase<OrderDto, ConfirmOrderOnFiscalRegistrar.ConfirmOrderOnFiscalRegistrarDto>
    {
        public ConfirmOrderOnFiscalRegistrar(int id, string fiscalId, bool completedOnFiscalRegistrar)
                : base(id, new ConfirmOrderOnFiscalRegistrarDto(id, fiscalId, completedOnFiscalRegistrar), ApiResources.Orders, "confirm_on_fiscal_registrar")
        {
        }

        public class ConfirmOrderOnFiscalRegistrarDto
        {
            public ConfirmOrderOnFiscalRegistrarDto(int orderId, string fiscalId, bool completedOnFiscalRegistrar)
            {
                OrderId = orderId;
                FiscalId = fiscalId;
                CompletedOnFiscalRegistrar = completedOnFiscalRegistrar;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("fiscal_id")]
            public string FiscalId { get; set; }

            [JsonProperty("completed_on_fiscal_registrar")]
            public bool CompletedOnFiscalRegistrar { get; set; }
        }
    }
}
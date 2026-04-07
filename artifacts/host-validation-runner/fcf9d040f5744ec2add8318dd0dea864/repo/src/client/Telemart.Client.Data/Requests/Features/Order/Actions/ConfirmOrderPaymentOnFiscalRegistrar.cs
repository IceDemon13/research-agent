using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class ConfirmOrderPaymentOnFiscalRegistrar : CallEntityActionWithBodyRequestResultBase<OrderPaymentDto, ConfirmOrderPaymentOnFiscalRegistrar.ConfirmOrderPaymentOnFiscalRegistrarDto>
    {
        public ConfirmOrderPaymentOnFiscalRegistrar(int orderPaymentId, string fiscalId, bool completedOnFiscalRegistrar, int fiscalCashboxId)
            : base(
                orderPaymentId,
                new ConfirmOrderPaymentOnFiscalRegistrarDto(orderPaymentId, fiscalId, completedOnFiscalRegistrar, fiscalCashboxId),
                $"{ApiResources.Orders}/order_payments",
                "confirm_on_fiscal_registrar")
        {
        }

        public class ConfirmOrderPaymentOnFiscalRegistrarDto
        {
            public ConfirmOrderPaymentOnFiscalRegistrarDto(int orderPaymentId, string fiscalId, bool completedOnFiscalRegistrar, int fiscalCashboxId)
            {
                OrderPaymentId = orderPaymentId;
                FiscalId = fiscalId;
                CompletedOnFiscalRegistrar = completedOnFiscalRegistrar;
                FiscalCashboxId = fiscalCashboxId;
            }

            [JsonProperty("order_payment_id")]
            public int OrderPaymentId { get; init; }

            [JsonProperty("fiscal_id")]
            public string FiscalId { get; init; }

            [JsonProperty("completed_on_fiscal_registrar")]
            public bool CompletedOnFiscalRegistrar { get; init; }

            [JsonProperty("fiscal_cashbox_id")]
            public int FiscalCashboxId { get; init; }
        }
    }
}
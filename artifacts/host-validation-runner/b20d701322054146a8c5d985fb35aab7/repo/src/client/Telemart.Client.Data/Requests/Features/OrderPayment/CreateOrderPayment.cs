using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Terminal;

namespace Telemart.Client.Data.Requests.Features.OrderPayment
{
    public sealed class CreateOrderPayment : CallEntityActionWithBodyRequestResultBase<OrderPaymentResultDto, OrderPaymentCreateDto>
    {
        public CreateOrderPayment(
            int orderId,
            int currencyId,
            int? paymentId,
            int cashboxId,
            decimal amount,
            DateTime? receivedOn,
            string comment,
            TerminalDataDto terminalData = null,
            decimal? codComission = null,
            int? fiscalCahboxId = null,
            bool prepayment = true)
        : base(
            orderId,
            new OrderPaymentCreateDto(cashboxId, currencyId, amount, paymentId, comment, receivedOn, terminalData, codComission, fiscalCahboxId, prepayment),
            ApiResources.Orders,
            "pay")
        {
        }
    }
}
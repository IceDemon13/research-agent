using System;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Terminal;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderPaymentCreateDto
    {
        public OrderPaymentCreateDto(
            int cashboxId,
            int currencyId,
            decimal amount,
            int? paymentId,
            string comment,
            DateTime? receivedOn,
            TerminalDataDto terminalDataDto,
            decimal? codComission,
            int? fiscalCashboxId,
            bool? prepayment)
        {
            CashboxId = cashboxId;
            CurrencyId = currencyId;
            Amount = amount;
            PaymentId = paymentId;
            Comment = comment;
            ReceivedOn = receivedOn;
            CodCommission = codComission;
            TerminalDataDto = terminalDataDto;
            FiscalCashboxId = fiscalCashboxId;
            Prepayment = prepayment;
        }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; set; }

        [JsonProperty("cod_commission")]
        public decimal? CodCommission { get; set; }

        [JsonProperty("terminal_data")]
        public TerminalDataDto TerminalDataDto { get; set; }

        [JsonProperty("fiscal_cashbox_id")]
        public int? FiscalCashboxId { get; set; }

        [JsonProperty("prepayment")]
        public bool? Prepayment { get; set; }
    }
}
namespace Telemart.Client.Dictionaries
{
    public sealed class Payment : DictionaryItem
    {
        public const int CashId = 1;
        public const int BankId = 2;
        public const int WmzId = 3;
        public const int WmuId = 4;
        public const int CashlessTaxId = 5;
        public const int CashlessNoTaxId = 6;
        public const int NoId = 7;
        public const int BitcoinId = 8;
        public const int CreditId = 9;
        public const int LiqPayId = 10;
        public const int TerminalId = 11;
        public const int MonobankId = 12;
        public const int PortmoneId = 13;
        public const int AlfabankId = 14;
        public const int IdeabankId = 15;
        public const int PaylaterId = 16;
        public const int PrivatPartialPayId = 17;
        public const int BonusesId = 18;
        public const int MonoPayId = 19;
        public const int NovaPayId = 20;
        public const int PumbId = 21;
        public const int ABankId = 22;

        public Payment(
            int id,
            string name,
            string nameUa,
            string nameEn,
            bool active,
            bool credit,
            bool partialCredit,
            int[] refundPaymentIds,
            bool autoFillSources,
            bool onlyCreateOnWeb,
            decimal limitUah,
            decimal? minLimitUah,
            decimal limitUsd,
            bool refundRevertAllowed,
            bool smsTemplate = false,
            bool refundRequisitesControl = false,
            bool canEditProducts = true,
            bool fiscal = true)
            : base(id, name, active)
        {
            NameUa = nameUa;
            NameEn = nameEn;
            AutoFillSources = autoFillSources;
            OnlyCreateOnWeb = onlyCreateOnWeb;
            RefundPaymentIds = refundPaymentIds;
            SmsTemplate = smsTemplate;
            RefundRequisitesControl = refundRequisitesControl;
            CanEditProducts = canEditProducts;
            LimitUah = limitUah;
            MinLimitUah = minLimitUah;
            LimitUsd = limitUsd;
            Credit = credit;
            PartialCredit = partialCredit;
            RefundRevertAllowed = refundRevertAllowed;
            Fiscal = fiscal;
        }

        public string NameUa { get; }

        public string NameEn { get; }

        public bool AutoFillSources { get; }

        public bool OnlyCreateOnWeb { get; }

        public bool CanEditProducts { get; }

        public bool PartialCredit { get; }

        public bool Credit { get; }

        public bool SmsTemplate { get; }

        public int[] RefundPaymentIds { get; }

        public bool RefundRequisitesControl { get; }

        public bool RefundRevertAllowed { get; }

        public decimal LimitUah { get; }

        public decimal? MinLimitUah { get; }

        public decimal LimitUsd { get; }

        public bool Fiscal { get; }

        public static bool IsCreditPayment(int? paymentId)
        {
            return paymentId == CreditId
                   || paymentId == MonobankId
                   || paymentId == PumbId
                   || paymentId == ABankId
                   || paymentId == PrivatPartialPayId
                   || paymentId == AlfabankId
                   || paymentId == IdeabankId
                   || paymentId == PaylaterId;
        }

        public static bool IsEditingAllowed(int? paymentId, int? paymentStateId, bool? credit, bool? partialCredit)
        {
            return paymentId is null
                   || paymentStateId is null
                   || !PaymentState.IsActual(paymentStateId.Value)
                   || credit == false
                   || partialCredit == true;
        }

        public static  bool IsCachlessPayment(int? paymentId)
        {
            return paymentId == CashlessTaxId || paymentId == CashlessNoTaxId;
        }
    }
}
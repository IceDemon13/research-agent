namespace Telemart.Client.Dictionaries
{
    public class SmsTemplate : DictionaryItem
    {
        public const int RefundNotifyId = 1000;
        public const int ServiceRequestRefundId = 1001;
        public const int ServiceRequestNotReachedId = 1002;
        public const int ServiceRequestTtnId = 1003;
        public const int ServiceRequestNumberId = 1004;
        public const int ServiceRequestProductPickupId = 1005;
        public const int ServiceRequestCardRefundId = 1006;
        public const int OrderCancelId = 1100;
        public const int OrderNotWhitePaymentId = 1101;
        public const int OrderNumberId = 1102;
        public const int CouldNotContactId = 1103;
        public const int OrderWhitePaymentId = 1104;
        public const int LiqPayOrderPrepaymentId = 1105;
        public const int OrderNotWhiteTuzPaymentId = 1106;
        public const int MonoPayOrderPrepaymentId = 1205;
        public const int PrivatPartialOrderPrepaymentId = 1210;
        public const int PrivatCreditOrderPrepaymentId = 1211;
        public const int NovaPayOrderPrepaymentId = 1212;
        public const int PortmoneOrderPrepaymentId = 1217;
        public const int NovaPayOrderPrepaymentNovakLegalEntityId = 2008;
        public const int RelevanceClarificationId = 2009;
        public const int NovaPayOrderPrepaymentTkachLegalEntityId = 2012;

        public SmsTemplate(int id, string name, string smsText, string viberText, int? legalEntityId, bool showInOrder, bool showInServiceRequest, int position)
            : base(id, name, true)
        {
            SmsText = smsText;
            ViberText = viberText;
            LegalEntityId = legalEntityId;
            ShowInOrder = showInOrder;
            ShowInServiceRequest = showInServiceRequest;
            Position = position;
        }

        public string SmsText { get; }

        public string ViberText { get; }

        public int? LegalEntityId { get; }

        public bool ShowInOrder { get; }

        public bool ShowInServiceRequest { get; }

        public int Position { get; }
    }
}
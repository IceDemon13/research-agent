namespace Telemart.Client.Dictionaries
{
    public class FiscalDocumentType : DictionaryItem
    {
        private const int ReceiveId = 1;
        private const int RefundId = 2;
        private const int CashCollectionId = 3;
        private const int ServiceReceiveId = 4;
        private const int ZReportId = 5;

        public FiscalDocumentType(int id, string name)
            : base(id, name, true)
        {
        }

        public static FiscalDocumentType Receive { get; } = new FiscalDocumentType(ReceiveId, "Оплата");

        public static FiscalDocumentType Refund { get; } = new FiscalDocumentType(RefundId, "Возврат");

        public static FiscalDocumentType CashCollection { get; } = new FiscalDocumentType(CashCollectionId, "Инкассация");

        public static FiscalDocumentType ServiceReceive { get; } = new FiscalDocumentType(ServiceReceiveId, "Служебное внесение");

        public static FiscalDocumentType ZReport { get; } = new FiscalDocumentType(ZReportId, "Z отчет");
    }
}

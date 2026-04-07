namespace Telemart.Client.Dictionaries
{
    public class SupplierBillDocumentType : DictionaryItem
    {
        private const int BillId = 1;
        private const int InvoiceId = 2;

        private SupplierBillDocumentType(int id, string name)
            : base(id, name, true)
        {
        }

        public static SupplierBillDocumentType Bill { get; } = new SupplierBillDocumentType(BillId, "Счет");

        public static SupplierBillDocumentType Invoice { get; } = new SupplierBillDocumentType(InvoiceId, "Расходная накладная");
    }
}

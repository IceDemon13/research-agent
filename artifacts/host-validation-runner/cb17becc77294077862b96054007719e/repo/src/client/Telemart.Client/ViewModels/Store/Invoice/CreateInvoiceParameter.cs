namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class CreateInvoiceParameter
    {
        public CreateInvoiceParameter(int supplierId)
        {
            SupplierId = supplierId;
        }

        public int SupplierId { get; set; }
    }
}

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceProductBulkAddParameter
    {
        public InvoiceProductBulkAddParameter(int contractorId)
        {
            ContractorId = contractorId;
        }

        public int ContractorId { get; }
    }
}
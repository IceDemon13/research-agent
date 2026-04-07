namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class EditInvoiceWarehouseParameter
    {
        public EditInvoiceWarehouseParameter(int warehouseId)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}
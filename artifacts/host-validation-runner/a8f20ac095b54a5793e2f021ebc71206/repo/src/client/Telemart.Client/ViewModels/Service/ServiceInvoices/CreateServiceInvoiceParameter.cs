namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class CreateServiceInvoiceParameter
    {
        public CreateServiceInvoiceParameter()
        {
        }

        public CreateServiceInvoiceParameter(int serviceCenterId, int warehouseId)
        {
            WarehouseId = warehouseId;
            ServiceCenterId = serviceCenterId;
        }

        public int? WarehouseId { get; }

        public int? ServiceCenterId { get; }
    }
}

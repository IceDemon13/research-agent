namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class SendServiceInvoiceParameter
    {
        public SendServiceInvoiceParameter(int serviceInvoiceId, int warehouseId, int? employeeCarrierId, string ttn, bool needEmployeeCarrier)
        {
            ServiceInvoiceId = serviceInvoiceId;
            WarehouseId = warehouseId;
            EmployeeCarrierId = employeeCarrierId;
            Ttn = ttn;
            NeedEmployeeCarrier = needEmployeeCarrier;
        }

        public int ServiceInvoiceId { get; }

        public int WarehouseId { get; }

        public int? EmployeeCarrierId { get; }

        public string Ttn { get; }

        public bool NeedEmployeeCarrier { get; }
    }
}
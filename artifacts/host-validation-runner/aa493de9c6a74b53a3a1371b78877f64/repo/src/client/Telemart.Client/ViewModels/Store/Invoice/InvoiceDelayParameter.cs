using System;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceDelayParameter
    {
        public InvoiceDelayParameter(int invoiceId, DateTime arriveDate, int? warehouseId)
        {
            InvoiceId = invoiceId;
            ArriveDate = arriveDate;
            WarehouseId = warehouseId;
        }

        public int InvoiceId { get; }

        public DateTime ArriveDate { get; }

        public int? WarehouseId { get; }
    }
}
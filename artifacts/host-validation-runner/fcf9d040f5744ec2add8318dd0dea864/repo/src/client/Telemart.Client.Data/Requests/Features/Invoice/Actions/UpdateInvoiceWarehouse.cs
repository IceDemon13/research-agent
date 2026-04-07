using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class UpdateInvoiceWarehouse : CallEntityActionWithBodyRequestResultBase<InvoiceDto, UpdateInvoiceWarehouse.InvoiceUpdateWarehouseDto>
    {
        public UpdateInvoiceWarehouse(int invoiceId, int warehouseId)
            : base(invoiceId, new InvoiceUpdateWarehouseDto(warehouseId), ApiResources.Invoices, "change_warehouse")
        {
        }

        public sealed class InvoiceUpdateWarehouseDto
        {
            public InvoiceUpdateWarehouseDto(int warehouseId)
            {
                WarehouseId = warehouseId;
            }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; set; }
        }
    }
}
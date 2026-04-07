using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PurchaseSourceSaveDto
    {
        private const int WarehouseSourceId = 1;
        private const int PurchaseSourceId = 2;
        private const int MovementSourceId = 3;
        private const int NoProductSourceId = 4;

        private PurchaseSourceSaveDto(
            int sourceId,
            string sourceText,
            DateTime? sourceDate,
            int? warehouseId,
            int? invoiceId,
            int? movementId)
        {
            SourceId = sourceId;
            SourceText = sourceText;
            SourceDate = sourceDate;
            WarehouseId = warehouseId;
            InvoiceId = invoiceId;
            MovementId = movementId;
        }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; set; }

        [JsonProperty("source_id")]
        public int SourceId { get; set; }

        [JsonProperty("source_text")]
        public string SourceText { get; set; }

        [JsonProperty("source_date")]
        public DateTime? SourceDate { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [JsonProperty("invoice_id")]
        public int? InvoiceId { get; set; }

        [JsonProperty("movement_id")]
        public int? MovementId { get; set; }

        [JsonProperty("without_stock_white")]
        public bool? WithoutStockWhite { get; set; }

        [JsonProperty("allow_locked_by_me")]
        public bool AllowLockedByMe { get; set; }

        public static PurchaseSourceSaveDto Warehouse(int warehouseId, string warehouseName, bool? withoutStockWhite)
        {
            return new PurchaseSourceSaveDto(
                WarehouseSourceId,
                $"Склад: {warehouseName}",
                DateTime.Now,
                warehouseId,
                null,
                null)
            {
                WithoutStockWhite = withoutStockWhite
            };
        }

        public static PurchaseSourceSaveDto Movement(
            int movementId,
            string warehouseFromName,
            int warehouseToId,
            string warehouseToName,
            DateTime dateOut,
            DateTime dateIn)
        {
            string sourceText = $"{warehouseFromName} {dateOut:dd.MM.yy HH:mm} => {warehouseToName} {dateIn:dd.MM.yy HH:mm} (уехало)";

            return new PurchaseSourceSaveDto(
                MovementSourceId,
                sourceText,
                dateIn,
                warehouseToId,
                null,
                movementId);
        }

        public static PurchaseSourceSaveDto Purchase(
            int warehouseId,
            string warehouseName,
            int invoiceId,
            string carryTypeName,
            string supplierName,
            string supplierWarehouseName,
            DateTime dateClose,
            DateTime dateGet)
        {
            string timeInterval = dateClose.Date == dateGet.Date
                ? $"{dateClose:dd.MM.yy} {dateClose:HH:mm}-{dateGet:HH:mm}"
                : $"{dateClose:dd.MM.yy HH:mm}-{dateGet:dd.MM.yy HH:mm}";

            string sourceText = $"{supplierName} {supplierWarehouseName}.{carryTypeName} {timeInterval} {warehouseName}";

            return new PurchaseSourceSaveDto(
                PurchaseSourceId,
                sourceText,
                dateGet,
                warehouseId,
                invoiceId,
                null);
        }

        public static PurchaseSourceSaveDto NoProduct(string alternativeText, int orderProductId = 0)
        {
            return new PurchaseSourceSaveDto(
                NoProductSourceId,
                alternativeText,
                null,
                null,
                null,
                null)
            {
                OrderProductId = orderProductId
            };
        }
    }
}
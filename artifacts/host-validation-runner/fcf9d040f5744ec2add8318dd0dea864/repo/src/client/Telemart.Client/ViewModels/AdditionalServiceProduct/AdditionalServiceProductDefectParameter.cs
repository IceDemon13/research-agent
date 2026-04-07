using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public sealed class AdditionalServiceProductDefectParameter : EditorParameter
    {
        public AdditionalServiceProductDefectParameter(
            int additionalServiceProductId,
            string productName,
            string serialNumber,
            bool keepSerial,
            int additionalServiceId,
            int warehouseId,
            int orderId)
            : base(additionalServiceProductId)
        {
            SerialNumber = serialNumber;
            ProductName = productName;
            KeepSerial = keepSerial;
            AdditionalServiceId = additionalServiceId;
            WarehouseId = warehouseId;
            OrderId = orderId;
        }

        public string SerialNumber { get; }

        public string ProductName { get; }

        public bool KeepSerial { get; }

        public int AdditionalServiceId { get; }

        public int WarehouseId { get; }

        public int OrderId { get; }
    }
}
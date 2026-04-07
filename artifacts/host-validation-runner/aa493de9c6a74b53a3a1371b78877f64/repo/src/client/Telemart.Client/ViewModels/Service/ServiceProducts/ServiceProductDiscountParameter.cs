namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public class ServiceProductDiscountParameter
    {
        public ServiceProductDiscountParameter(int serviceProductId, int productId, string serialNumber, int warehouseId, int serviceProductTypeId, int? serviceRequestId = null, bool defectCreateDiscount = false)
        {
            ServiceProductId = serviceProductId;
            ProductId = productId;
            SerialNumber = serialNumber;
            WarehouseId = warehouseId;
            ServiceProductTypeId = serviceProductTypeId;
            ServiceRequestId = serviceRequestId;
            DefectCreateDiscount = defectCreateDiscount;
        }

        public int ServiceProductId { get; }

        public int ServiceProductTypeId { get; }

        public int ProductId { get; }

        public string SerialNumber { get; }

        public int WarehouseId { get; }

        public bool DefectCreateDiscount { get; }

        public int? ServiceRequestId { get; }
    }
}
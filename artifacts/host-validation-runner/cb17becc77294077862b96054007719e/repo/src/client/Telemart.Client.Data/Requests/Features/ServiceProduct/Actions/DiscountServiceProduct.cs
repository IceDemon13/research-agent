using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class DiscountServiceProduct : CallEntityActionWithBodyRequestResultBase<ServiceProductDto, DiscountServiceProduct.ServiceProductDiscountDto>
    {
        public DiscountServiceProduct(
            int serviceProductId,
            string serialNumber,
            int warehouseId,
            int productDiscountId,
            bool defectCreateDiscount)
            : base(serviceProductId, new ServiceProductDiscountDto(serviceProductId, serialNumber, warehouseId, productDiscountId, defectCreateDiscount), ApiResources.ServiceProducts, "discount")
        {
        }

        public class ServiceProductDiscountDto
        {
            public ServiceProductDiscountDto(int id, string serialNumber, int warehouseId, int productDiscountId, bool defectCreateDiscount)
            {
                Id = id;
                SerialNumber = serialNumber;
                WarehouseId = warehouseId;
                ProductDiscountId = productDiscountId;
                DefectCreateDiscount = defectCreateDiscount;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("serial_number")]
            public string SerialNumber { get; set; }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; set; }

            [JsonProperty("product_discount_id")]
            public int ProductDiscountId { get; set; }

            [JsonProperty("defect_create_discount")]
            public bool DefectCreateDiscount { get; set; }
        }
    }
}
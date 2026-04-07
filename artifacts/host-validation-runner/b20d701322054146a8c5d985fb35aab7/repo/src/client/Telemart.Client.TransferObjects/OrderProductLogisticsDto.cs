using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductLogisticsDto
    {
        public OrderProductLogisticsDto(
            int id,
            DateTime sourceDate,
            int warehouseId,
            int? assembliesCount,
            int? orderFolderId,
            int? parentRecordId,
            bool isAdditionalService,
            bool isAssemblyIncluded,
            int? quantity,
            int? additionalServiceId,
            int productId,
            string productName,
            bool isGuestProduct)
        {
            Id = id;
            SourceDate = sourceDate;
            WarehouseId = warehouseId;
            AssembliesCount = assembliesCount;
            OrderFolderId = orderFolderId;
            ParentRecordId = parentRecordId;
            IsAdditionalService = isAdditionalService;
            IsAssemblyIncluded = isAssemblyIncluded;
            Quantity = quantity;
            AdditionalServiceId = additionalServiceId;
            ProductId = productId;
            ProductName = productName;
            IsGuestProduct = isGuestProduct;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("source_date")]
        public DateTime SourceDate { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("assemblies_count")]
        public int? AssembliesCount { get; set; }

        [JsonProperty("order_folder_id")]
        public int? OrderFolderId { get; set; }

        [JsonProperty("parent_record_id")]
        public int? ParentRecordId { get; set; }

        [JsonProperty("is_additional_service")]
        public bool IsAdditionalService { get; set; }

        [JsonProperty("assembly_included")]
        public bool IsAssemblyIncluded { get; set; }

        [JsonProperty("quantity")]
        public int? Quantity { get; set; }

        [JsonProperty("additional_service_id")]
        public int? AdditionalServiceId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("is_guest_product")]
        public bool IsGuestProduct { get; set; }
    }
}
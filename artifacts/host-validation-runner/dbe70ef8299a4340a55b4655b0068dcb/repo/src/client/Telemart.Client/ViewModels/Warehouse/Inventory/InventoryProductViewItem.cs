using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class InventoryProductViewItem
    {
        protected InventoryProductViewItem()
        {
        }

        public virtual int? Id { get; set; }

        public virtual int? AssemblyServiceId { get; set; }

        public virtual int InventoryId { get; set; }

        public virtual string FullName { get; set; }

        public virtual int? ParentCategoryId { get; set; }

        public virtual string ParentCategoryName { get; set; }

        public virtual int ProductId { get; set; }

        public virtual int Quantity { get; set; }

        public virtual int QuantityReal { get; set; }

        public virtual int CurrentInventoryQuantity { get; set; }

        public virtual int CurrentInventoryQuantityReal { get; set; }

        public static InventoryProductViewItem Create(int? inventoryProductId, int inventoryId, int productId, string fullName, string parentCategoryName, int quantity, int quantityReal, int? parentCategoryId, int? assemblyServiceId = null)
        {
            InventoryProductViewItem item = Create();
            item.Id = inventoryProductId;
            item.AssemblyServiceId = assemblyServiceId;
            item.InventoryId = inventoryId;
            item.ProductId = productId;
            item.FullName = fullName;
            item.ParentCategoryName = parentCategoryName;
            item.ParentCategoryId = parentCategoryId;
            item.QuantityReal = quantityReal;
            item.Quantity = quantity;

            return item;
        }

        public static void BuildMetadata(MetadataBuilder<InventoryProductViewItem> builder)
        {
            builder.Property(x => x.QuantityReal).MatchesRule(x => x >= 0, () => "Значение должно быть больше или равно 0");
        }

        public static InventoryProductViewItem Create()
        {
            return ViewModelSource<InventoryProductViewItem>.Create();
        }
    }
}
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseViewItem : TelemartEditorViewItemBase
    {
        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int ProductParentCategoryId
        {
            get { return GetProperty(() => ProductParentCategoryId); }
            set { SetProperty(() => ProductParentCategoryId, value); }
        }

        public int? Capacity
        {
            get { return GetProperty(() => Capacity); }
            set { SetProperty(() => Capacity, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ShowcaseViewItem> builder)
        {
            builder.Property(x => x.WarehouseId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ProductName).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Capacity).Required(() => Resources.RequiredErrorMessage);
        }
    }
}

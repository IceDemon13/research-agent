using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Content.ProductEquipments
{
    public class ProductEquipmentViewItem : BindableBase
    {
        public string ExcelProductName
        {
            get { return GetProperty(() => ExcelProductName); }
            set { SetProperty(() => ExcelProductName, value); }
        }

        public string Equipment
        {
            get { return GetProperty(() => Equipment); }
            set { SetProperty(() => Equipment, value); }
        }

        public string EquipmentUkr
        {
            get { return GetProperty(() => EquipmentUkr); }
            set { SetProperty(() => EquipmentUkr, value); }
        }

        public string EquipmentEn
        {
            get { return GetProperty(() => EquipmentEn); }
            set { SetProperty(() => EquipmentEn, value); }
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

        public ProductInfoType Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ProductEquipmentViewItem> builder)
        {
            builder.Property(x => x.ProductId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ProductName).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
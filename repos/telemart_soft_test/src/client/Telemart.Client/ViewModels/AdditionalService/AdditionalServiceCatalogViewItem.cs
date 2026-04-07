using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServiceCatalogViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value, () => { RaisePropertiesChanged(nameof(DisplayProductName)); }); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            set { SetProperty(() => ProductNameUkr, value, () => { RaisePropertiesChanged(nameof(DisplayProductName)); }); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value, () => { RaisePropertiesChanged(nameof(DisplayProductName)); }); }
        }

        public string ProductLink
        {
            get { return GetProperty(() => ProductLink); }
            set { SetProperty(() => ProductLink, value); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public decimal MinPrice
        {
            get { return GetProperty(() => MinPrice); }
            set { SetProperty(() => MinPrice, value); }
        }

        public decimal Percent
        {
            get { return GetProperty(() => Percent); }
            set { SetProperty(() => Percent, value); }
        }

        public bool AssemblyPart
        {
            get { return GetProperty(() => AssemblyPart); }
            set { SetProperty(() => AssemblyPart, value); }
        }

        public bool AdditionalWarranty
        {
            get { return GetProperty(() => AdditionalWarranty); }
            set { SetProperty(() => AdditionalWarranty, value); }
        }

        public bool AutoAdd
        {
            get { return GetProperty(() => AutoAdd); }
            set { SetProperty(() => AutoAdd, value); }
        }

        public string DisplayProductName => Extensions.EntityLocalіzerExtensions.GetLacalString(ProductName, ProductNameUkr, ProductNameEn, LocalizableNameType.Ukr);
    }
}
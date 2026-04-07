using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures.YandexMarketFeaturesParser
{
    public class ProductFeaturesYandexMarketProductViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string YandexId
        {
            get { return GetProperty(() => YandexId); }
            set { SetProperty(() => YandexId, value); }
        }

        public int YandexCategoryHid
        {
            get { return GetProperty(() => YandexCategoryHid); }
            set { SetProperty(() => YandexCategoryHid, value); }
        }

        public bool EmptyFeatures
        {
            get { return GetProperty(() => EmptyFeatures); }
            set { SetProperty(() => EmptyFeatures, value); }
        }
    }
}

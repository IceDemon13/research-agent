using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures.YandexMarketFeaturesParser
{
    public class ProductFeaturesYandexMarketParseViewModel : TelemartDialogViewModelBase
    {
        private Dictionary<string, string> featureMap;

        public ProductFeaturesYandexMarketParseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ProductChangedCommand = new DelegateCommand(RaiseProperties);
            SelectEmptyFeaturesProductsCommand = new DelegateCommand(SelectEmptyFeaturesProducts);
        }

        public IDelegateCommand ProductChangedCommand { get; }

        public IDelegateCommand SelectEmptyFeaturesProductsCommand { get; }

        public ObservableCollection<CheckableItem<ProductFeaturesYandexMarketProductViewItem>> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public bool AnyProductsNotSelected => Products?.Any(x => x.IsChecked) != true;

        public string SelectedProductsCountText => GetSelectedProductsCountText();

        public IReadOnlyCollection<ProductFeaturesYandexMarketProductViewItem> SelectedProducts { get; private set; }

        public bool? CheckAllState
        {
            get
            {
                return Products?.All(r => r.IsChecked) == true ? true :
                    (Products?.Any(r => r.IsChecked) == true ? (bool?)null : false);
            }

            set
            {
                foreach (CheckableItem<ProductFeaturesYandexMarketProductViewItem> product in Products)
                {
                    product.IsChecked = value ?? false;
                }

                RaiseProperties();
            }
        }

        public IReadOnlyCollection<Dictionary<string, object>> ParsedProductFeatures { get; private set; }

        protected override Task HandleLoadedAsync()
        {
            ProductFeaturesYandexMarketParseParameter parameter = (ProductFeaturesYandexMarketParseParameter)Parameter;

            featureMap = parameter.FeatureMap;

            Products = parameter.Products.Select(x => new CheckableItem<ProductFeaturesYandexMarketProductViewItem>(x, x.EmptyFeatures)).OrderBy(x => x.Item.Name).ToObservableCollection();

            Title = "Выберите товары";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;

            SelectedProducts = Products.Where(x => x.IsChecked).Select(x => x.Item).ToArray();

            ProductFeaturesYandexMarketParseParameter parserParameter = new ProductFeaturesYandexMarketParseParameter(
                SelectedProducts,
                featureMap);

            ProductFeaturesYandexMarketParseReportViewModel parserReportViewModel = DialogDocumentManagerService.ShowView<ProductFeaturesYandexMarketParseReportViewModel>(parserParameter, this);

            if (parserReportViewModel.IsOk)
            {
                ParsedProductFeatures = parserReportViewModel.ParsedProductFeatures;
                Close();
            }

            return Task.CompletedTask;
        }

        private void SelectEmptyFeaturesProducts()
        {
            Products.ForEach(x => x.IsChecked = x.Item.EmptyFeatures);
            RaiseProperties();
        }

        private string GetSelectedProductsCountText()
        {
            string seletedProductsCountText = string.Empty;

            if (Products?.Any() == true)
            {
                int selectedProductsCount = Products.Count(x => x.IsChecked);

                if (selectedProductsCount > 0)
                {
                    string selectText = WordEndingHelper.GetWordByNumber(selectedProductsCount, "Выбран", "Выбрано", "Выбрано");
                    string productsText = WordEndingHelper.GetWordByNumber(selectedProductsCount, "товар", "товара", "товаров");

                    seletedProductsCountText = $"{selectText} {selectedProductsCount} {productsText}";
                }
                else
                {
                    seletedProductsCountText = "Товары не выбраны";
                }
            }

            return seletedProductsCountText;
        }

        private void RaiseProperties()
        {
            RaisePropertiesChanged(nameof(CheckAllState), nameof(SelectedProductsCountText), nameof(AnyProductsNotSelected));
        }
    }
}

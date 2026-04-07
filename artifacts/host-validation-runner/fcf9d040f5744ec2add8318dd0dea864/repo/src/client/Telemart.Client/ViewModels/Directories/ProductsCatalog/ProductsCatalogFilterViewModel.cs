using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public sealed class ProductsCatalogFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private List<CategoryDto> categoriesList;

        public ProductsCatalogFilterViewModel(IWebClient webClient, IDictionaries dictionaries, IMapper mapper)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AvailTypes = Dictionaries.GetItems<ProductAvailability>()
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableRangeCollection();

            Categories = new ObservableRangeCollection<CategoryViewItem>();

            SelectedAvailTypes = AvailTypes.Where(x => x.Id != ProductAvailability.Archive.Id).ToObservableRangeCollection();
        }

        #region INPC

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> AvailTypes
        {
            get { return GetProperty(() => AvailTypes); }
            set { SetProperty(() => AvailTypes, value); }
        }

        public ObservableRangeCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedAvailTypes
        {
            get { return GetProperty(() => SelectedAvailTypes); }
            set { SetProperty(() => SelectedAvailTypes, value); }
        }

        public CategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        #endregion

        public string Error => string.Empty;

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private IMapper Mapper { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public Task RefreshAsync()
        {
            return RefreshCategoriesAsync();
        }

        public void ResetFilterValues()
        {
            Name = null;
            SelectedCategory = null;
            SelectedAvailTypes.Clear();
        }

        public ProductCatalogFilteringItem GetFilteringItem()
        {
            ProductCatalogFilteringItem item = new ProductCatalogFilteringItem();

            item.Name = Name;
            item.AvailTypes = SelectedAvailTypes.Select(x => x.Id).ToList();

            if (SelectedCategory != null)
            {
                item.Categories = new List<int> { SelectedCategory.Id };
            }

            return item;
        }

        public void Clear()
        {
            SelectedAvailTypes.Clear();
            SelectedCategory = null;
            Name = null;
        }

        private async Task RefreshCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            categories = categories.Where(x => WebClient.AuthenticatedEmployee.AllowCategories.Contains(x.Id)).ToList();

            if (ReferenceEquals(categories, categoriesList))
            {
                return;
            }

            CategoryViewItem selected = SelectedCategory;

            Categories.Clear();
            categoriesList = categories;
            Categories.AddRange(categoriesList.OrderBy(x => x.Position).Select(x => Mapper.Map<CategoryViewItem>(x)));
            RaisePropertyChanged(nameof(Categories));
            if (selected != null)
            {
                SelectedCategory = Categories.FirstOrDefault(x => x.Id == selected.Id);
            }
        }
    }
}
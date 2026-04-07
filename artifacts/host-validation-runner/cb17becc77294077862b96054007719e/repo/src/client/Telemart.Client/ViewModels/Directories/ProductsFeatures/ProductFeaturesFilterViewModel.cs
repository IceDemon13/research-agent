using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures
{
    public sealed class ProductFeaturesFilterViewModel : BindableBase
    {
        private List<CategoryDto> categoriesList;

        public ProductFeaturesFilterViewModel(IWebClient webClient, IDictionaries dictionaries, IMapper mapper)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AvailTypes = Dictionaries.GetItems<ProductAvailability>()
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableRangeCollection();

            Categories = new ObservableRangeCollection<CategoryViewItem>();

            SelectedAvailTypes = new ObservableCollection<ComboBoxItem>();

            ResetFilterValues();
        }

        #region INPC

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

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private IMapper Mapper { get; }

        public Task RefreshAsync()
        {
            return RefreshCategoriesAsync();
        }

        public void ResetFilterValues()
        {
            SelectedAvailTypes.Clear();
            SelectedAvailTypes.AddRange(AvailTypes.Where(x => x.Id != ProductAvailability.Archive.Id));
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

            if (selected != null)
            {
                SelectedCategory = Categories.FirstOrDefault(x => x.Id == selected.Id);
            }
        }
    }
}
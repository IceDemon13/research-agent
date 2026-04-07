using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseCategoryCreateViewModel : TelemartDialogViewModelBase
    {
        public ShowcaseCategoryCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messagefacadeService)
            : base(webClient, dictionaries, messagefacadeService)
        {
            CreatePlaceNameCommand = new DelegateCommand(CreatePlaceName);
        }

        public IDelegateCommand CreatePlaceNameCommand { get; }

        public CategoryDto SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        public ComboBoxItem? SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public int? Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public string PlaceName
        {
            get { return GetProperty(() => PlaceName); }
            set { SetProperty(() => PlaceName, value); }
        }

        public ObservableCollection<string> PlaceNames
        {
            get { return GetProperty(() => PlaceNames); }
            private set { SetProperty(() => PlaceNames, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ShowcaseCategoryCreateViewModel> builder)
        {
            builder.Property(x => x.SelectedCategory)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRule(x => x is null || x.IsParent, () => "Категория должна быть родительской");
            builder.Property(x => x.SelectedWarehouse)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PlaceName)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Quantity)
                .MatchesRule(x => x > 0, () => "Значение должно быть больше 0");
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehouses.Data
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.ShowCase.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data
                .Where(x => x.Active > 0)
                .ToReadOnlyObservableCollection();

            ShowcaseCategoryCreateParameter parameter = (ShowcaseCategoryCreateParameter)Parameter;

            PlaceNames = parameter.PlaceNames.ToObservableCollection();

            Title = parameter.Title;

            if (parameter.SelectedShowcaseCategory is not null)
            {
                Map(parameter.SelectedShowcaseCategory);
            }
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }

        private void CreatePlaceName()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Укажите название",
                "Создание места",
                null,
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            string placeName = fromUserViewModel.Content.Trim();

            PlaceNames.Add(placeName);
            PlaceNames = PlaceNames.OrderBy(x => x).ToObservableCollection();
            PlaceName = placeName;
        }

        private void Map(ShowcaseCategoryViewItem selectedItem)
        {
            Quantity = selectedItem.Quantity;
            PlaceName = selectedItem.PlaceName;
            SelectedCategory = Categories.FirstOrDefault(x => x.Id == selectedItem.CategoryId);
            SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == selectedItem.WarehouseId);
        }
    }
}
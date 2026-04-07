using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Contractor.Warehouse;
using Telemart.Client.Data.Requests.Features.ParserSettings;
using Telemart.Client.Data.Requests.Features.ParserSettings.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ParserSettings;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public sealed class ParserSettingsViewModel : TelemartEditorViewModelBase<ParserSettingsDto, ParserSettingsParameter, ParserSettingsViewItem>
    {
        private bool canEdit;

        public ParserSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            CreateCategoryCommand = new DelegateCommand(CreateCategory);
            EditCategoryCommand = new DelegateCommand<ParserSettingsCategoryViewItem>(EditCategory, x => x != null);
            DeleteCategoryCommand = new DelegateCommand<ParserSettingsCategoryViewItem>(DeleteCategory, x => x != null);
            AddAvailabilityCommand = new DelegateCommand(() => Model.Availabilities.Add(new ParserSettingsAvailabilityViewItem()));
            DeleteAvailabilityCommand = new DelegateCommand<ParserSettingsAvailabilityViewItem>(x => Model.Availabilities.Remove(x), x => x != null);
            DeletePriceCommand = new DelegateCommand(DeletePrice, () => SelectedPrice != null);
            AddPriceCommand = new DelegateCommand(AddPrice);
        }

        public ParserSettingsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CreateCategoryCommand { get; }

        public IDelegateCommand EditCategoryCommand { get; }

        public IDelegateCommand DeleteCategoryCommand { get; }

        public IDelegateCommand AddAvailabilityCommand { get; }

        public IDelegateCommand DeleteAvailabilityCommand { get; }

        public IDelegateCommand DeletePriceCommand { get; }

        public IDelegateCommand AddPriceCommand { get; }

        #endregion

        public ReadOnlyObservableCollection<ProductAvailabilityType> Availalities
        {
            get { return GetProperty(() => Availalities); }
            set { SetProperty(() => Availalities, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ParserSettingsType> ParserSettingsTypes
        {
            get { return GetProperty(() => ParserSettingsTypes); }
            private set { SetProperty(() => ParserSettingsTypes, value); }
        }

        public ReadOnlyObservableCollection<ParserSettingsFileSheetsType> ParserSettingsFileSheetsTypes
        {
            get { return GetProperty(() => ParserSettingsFileSheetsTypes); }
            private set { SetProperty(() => ParserSettingsFileSheetsTypes, value); }
        }

        public ReadOnlyObservableCollection<ParserPriceType> ParserPriceTypes
        {
            get { return GetProperty(() => ParserPriceTypes); }
            private set { SetProperty(() => ParserPriceTypes, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public List<SupplierWarehouseDto> SupplierWarehouses
        {
            get { return GetProperty(() => SupplierWarehouses); }
            private set { SetProperty(() => SupplierWarehouses, value); }
        }

        public ReadOnlyObservableCollection<ParserSettingsFileSheetsRecognizeType> ParserSettingsFileSheetsRecognizeTypes
        {
            get { return GetProperty(() => ParserSettingsFileSheetsRecognizeTypes); }
            private set { SetProperty(() => ParserSettingsFileSheetsRecognizeTypes, value); }
        }

        public bool ExcelTabVisible
        {
            get { return GetProperty(() => ExcelTabVisible); }
            private set { SetProperty(() => ExcelTabVisible, value); }
        }

        public bool SheetsRecognizeVisible
        {
            get { return GetProperty(() => SheetsRecognizeVisible); }
            private set { SetProperty(() => SheetsRecognizeVisible, value); }
        }

        public ParserSettingsFilePriceViewItem SelectedPrice
        {
            get { return GetProperty(() => SelectedPrice); }
            set { SetProperty(() => SelectedPrice, value); }
        }

        #region DialogSettings

        public override int Height => 800;

        public override int MinHeight => 600;

        public override int MinWidth => 1000;

        public override int Width => 1100;

        #endregion

        protected override string CreatedActionMessage { get; } = "созданы";

        protected override string EntityName { get; } = "Настройки парсера";

        protected override string UpdatedActionMessage { get; } = "обновлены";

        protected override bool UseStandartPropertyValidation { get; set; } = false;

        protected override async Task HandleLoadedAsync()
        {
            ParserSettingsParameter parameter = (ParserSettingsParameter)Parameter;

            canEdit = WebClient.IsOperationAllowed(BusinessOperation.ContractorUpdateParserSettings);

            Availalities = Dictionaries.GetItems<ProductAvailabilityType>().ToReadOnlyObservableCollection();
            ParserSettingsTypes = Dictionaries.GetItems<ParserSettingsType>().ToReadOnlyObservableCollection();
            ParserSettingsFileSheetsTypes = Dictionaries.GetItems<ParserSettingsFileSheetsType>().ToReadOnlyObservableCollection();
            ParserPriceTypes = Dictionaries.GetItems<ParserPriceType>().ToReadOnlyObservableCollection();
            ParserSettingsFileSheetsRecognizeTypes = Dictionaries.GetItems<ParserSettingsFileSheetsRecognizeType>().ToReadOnlyObservableCollection();
            Currencies = Dictionaries.GetItems<Currency>().ToReadOnlyObservableCollection();

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .OrderBy(x => x.Left)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            SupplierWarehouses = await WebClient.ExecuteApiRequestAsync(new QueryContractorWarehouses(parameter.ContractorId ?? Model.ContractorId));

            ExcelTabVisible = Model.TypeId == ParserSettingsType.Excel.Id;
        }

        protected override void AfterSetData()
        {
            foreach (ParserSettingsCategoryViewItem categoryViewItem in Model.Categories)
            {
                MapCategory(categoryViewItem);
            }

            Model.Categories = Model.Categories
                .OrderBy(x => x.Categories.DefaultIfEmpty().Select(y => y?.Left).First())
                .ToObservableCollection();

            foreach (ParserSettingsCategoryViewItem categoryViewItem in ModelOriginal.Categories)
            {
                MapCategory(categoryViewItem);
            }

            ModelOriginal.Categories = ModelOriginal.Categories
                .OrderBy(x => x.Categories.DefaultIfEmpty().Select(y => y?.Left).First())
                .ToObservableCollection();

            Model.ParserSettingsFile ??= new ParserSettingsFileViewItem();

            Model.ParserSettingsFile.FileColumn ??= new ParserSettingsFileColumnViewItem();

            Model.ParserSettingsFile.FileColumn.CategoryRule1 ??= new ParserSettingsFileColumnCategoryRuleViewItem();

            Model.ParserSettingsFile.FileColumn.CategoryRule2 ??= new ParserSettingsFileColumnCategoryRuleViewItem();

            Model.ParserSettingsFile.FileColumn.CategoryRule3 ??= new ParserSettingsFileColumnCategoryRuleViewItem();

            Model.ParserSettingsFile.PropertyChanged += OnFileModelPropertyChanged;

            SheetsRecognizeVisible = Model.ParserSettingsFile.SheetsTypeId == ParserSettingsFileSheetsType.Selectively.Id;
        }

        protected override Task<Result<ParserSettingsDto>> CreateEntityAsync()
        {
            ParserSettingsDto dto = Mapper.Map<ParserSettingsDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new CreateParserSettings(dto));
        }

        protected override object CreateEntityMessage(ParserSettingsDto dto, MessageType messageType)
        {
            return new ParserSettingsMessage(dto, messageType);
        }

        protected override Task<ParserSettingsDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryParserSettings(id));
        }

        protected override Task<LockResponse<ParserSettingsDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockParserSettings(id));
        }

        protected override Task<LockResponse<ParserSettingsDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockParserSettings(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание настроек парсера";
        }

        protected override void SetEditTitle()
        {
            Title = $"Настройки парсера ({Model.Id})";
        }

        protected override void Close()
        {
            Model.ParserSettingsFile.PropertyChanged -= OnFileModelPropertyChanged;

            base.Close();
        }

        protected override async Task<bool> SaveAsync()
        {
            if (!IsPricesValid())
            {
                return false;
            }

            if (!IsAvailValid())
            {
                return false;
            }

            return await base.SaveAsync();
        }

        protected override Task<Result<ParserSettingsDto>> UpdateEntityAsync()
        {
            ParserSettingsDto dto = Mapper.Map<ParserSettingsDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdateParserSettings(Model.Id, dto));
        }

        protected override bool CanEdit()
        {
            return canEdit;
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            if (Model is null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(ParserSettingsViewItem.TypeId):
                    ExcelTabVisible = Model.TypeId == ParserSettingsType.Excel.Id;

                    break;
            }
        }

        private void OnFileModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ParserSettingsFileViewItem.SheetsTypeId):
                    SheetsRecognizeVisible = Model.ParserSettingsFile.SheetsTypeId == ParserSettingsFileSheetsType.Selectively.Id;

                    if (!SheetsRecognizeVisible)
                    {
                        Model.ParserSettingsFile.SheetsRecognizePattern = null;
                        Model.ParserSettingsFile.SheetsRecognizeTypeId = null;
                    }

                    break;
            }
        }

        private void CreateCategory()
        {
            ParserSettingsCategoryViewItem item = new ParserSettingsCategoryViewItem(Model.Id);

            ParserSettingsCategoryViewModel viewModel = DialogDocumentManagerService.ShowView<ParserSettingsCategoryViewModel>(item, this);

            if (viewModel.IsOk)
            {
                MapCategory(item);
                Model.Categories.Add(item);
            }
        }

        private void EditCategory(ParserSettingsCategoryViewItem item)
        {
            ParserSettingsCategoryViewItem editItem = ReflectionObjectCloner.Clone(item);

            editItem.CategoryIds = item.CategoryIds.ToObservableCollection();
            editItem.OkWords = item.OkWords.ToObservableCollection();
            editItem.StopWords = item.StopWords.ToObservableCollection();
            editItem.Replaces = item.Replaces.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            ParserSettingsCategoryViewModel viewModel = DialogDocumentManagerService.ShowView<ParserSettingsCategoryViewModel>(editItem, this);

            if (viewModel.IsOk)
            {
                MapCategory(editItem);
                item = ReflectionObjectCloner.Clone(editItem, () => item);
            }
        }

        private void DeleteCategory(ParserSettingsCategoryViewItem item)
        {
            Model.Categories.Remove(item);
        }

        private void MapCategory(ParserSettingsCategoryViewItem item)
        {
            item.Categories = Categories.Where(x => item.CategoryIds.Contains(x.Id)).ToObservableCollection();
        }

        private void AddPrice()
        {
            ParserSettingsFilePriceViewItem newItem = new();

            Model.ParserSettingsFile.FileColumn.Prices ??= new ObservableCollection<ParserSettingsFilePriceViewItem>();

            Model.ParserSettingsFile.FileColumn.Prices.Insert(0, newItem);

            SelectedPrice = newItem;
        }

        private void DeletePrice()
        {
            Model.ParserSettingsFile.FileColumn.Prices.Remove(SelectedPrice);
        }

        private bool IsPricesValid()
        {
            if (Model.TypeId != ParserSettingsType.Excel.Id)
            {
                return true;
            }

            if (Model.ParserSettingsFile.FileColumn.Prices?.Any() != true)
            {
                MessageFacadeService.ShowNotificationError("Не заданы настройки для цен");
                return false;
            }

            if (Model.ParserSettingsFile.FileColumn.Prices?.GroupBy(x => new { x.ParserPriceTypeId, x.CurrencyId }).Count() != Model.ParserSettingsFile.FileColumn.Prices?.Count)
            {
                MessageFacadeService.ShowNotificationError("Настройки колонок цен продублированы");
                return false;
            }

            return true;
        }

        private bool IsAvailValid()
        {
            if (Model.TypeId != ParserSettingsType.Excel.Id)
            {
                return true;
            }

            bool anyAvailabilities = Model.Availabilities?.Any() == true;
            bool availColumnFilled = Model.ParserSettingsFile.FileColumn.AvailColumnNumber is not null;

            if (anyAvailabilities && !availColumnFilled)
            {
                MessageFacadeService.ShowNotificationError("Сопоставление наличия задано, а номер столбца нет");
                return false;
            }

            if (!anyAvailabilities && availColumnFilled)
            {
                MessageFacadeService.ShowNotificationError("Номер столбца наличия задан, а сопоставление нет");
                return false;
            }

            if (!anyAvailabilities)
            {
                MessageFacadeService.ShowNotificationInfo("Наличие будет указано для всех товаров");
            }

            return true;
        }
    }
}
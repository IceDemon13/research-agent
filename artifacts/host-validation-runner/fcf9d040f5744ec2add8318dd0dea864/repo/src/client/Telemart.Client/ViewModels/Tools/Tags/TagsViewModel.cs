using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telemart.Client.Business.Tag;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Promo;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.Tag;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Promo;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Tools.Tags
{
    public sealed class TagsViewModel : TelemartViewModelBase
    {
        private List<WarehouseTagFormatDto> warehouseTagFormatSettings;
        private readonly ITelemartClientLogger _telemartClientLogger;

        public TagsViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ITagPrinterFactory tagPrinterFactory,
            IMapper mapper,
            IDictionaries dictionaries,
            ILogger<TagsViewModel> logger,
            ITelemartClientLogger telemartClientLogger)
            : base(webClient, dictionaries, messageFacadeService, logger)
        {
            TagPrinterFactory = tagPrinterFactory ?? throw new ArgumentNullException(nameof(tagPrinterFactory));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            ResetCommand = new DelegateCommand(Reset, CanReset);
            AddCommand = new AsyncCommand(AddAsync, CanAdd);
            RemoveCommand = new DelegateCommand(Remove, CanRemove);
            PrintCommand = new AsyncCommand(PrintAsync, CanPrint);
            FillCommand = new AsyncCommand(FillAsync, CanFill);
            SaveCommand = new DelegateCommand<TableView>(Save, CanSave);
            SendLogsCommand = new AsyncCommand(SendLogsAsync, CanSendLogs);

            Tags = new ObservableRangeCollection<ProductTagViewItem>();
            SelectedTags = new ObservableRangeCollection<ProductTagViewItem>();

            TagsCollectionView = CollectionViewSource.GetDefaultView(Tags);
            TagsCollectionView.SortDescriptions.Add(new SortDescription(nameof(ProductTagDto.Category), ListSortDirection.Ascending));
            TagsCollectionView.SortDescriptions.Add(new SortDescription(nameof(ProductTagDto.Name), ListSortDirection.Ascending));

            _telemartClientLogger = telemartClientLogger;
        }

        public TagsViewModel()
        {
        }

        #region INPC

        public ObservableRangeCollection<ProductTagViewItem> SelectedTags { get; }

        public WarehouseDto SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public ObservableRangeCollection<ProductTagViewItem> Tags { get; }

        public string Logs
        {
            get { return GetProperty(() => Logs); }
            set { SetProperty(() => Logs, value); }
        }

        public ICollectionView TagsCollectionView { get; }

        public ObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<TagFormat> TagFormats
        {
            get { return GetProperty(() => TagFormats); }
            set { SetProperty(() => TagFormats, value); }
        }

        public ReadOnlyObservableCollection<Language> Languages
        {
            get { return GetProperty(() => Languages); }
            private set { SetProperty(() => Languages, value); }
        }

        public Language SelectedLanguage
        {
            get { return GetProperty(() => SelectedLanguage); }
            set { SetProperty(() => SelectedLanguage, value); }
        }

        public ReadOnlyObservableCollection<ProductPriceKind> Prices
        {
            get { return GetProperty(() => Prices); }
            private set { SetProperty(() => Prices, value); }
        }

        public ProductPriceKind SelectedPrice
        {
            get { return GetProperty(() => SelectedPrice); }
            set { SetProperty(() => SelectedPrice, value); }
        }

        #endregion

        #region Commands

        public IAsyncCommand AddCommand { get; }

        public IAsyncCommand FillCommand { get; }

        public IAsyncCommand PrintCommand { get; }

        public IDelegateCommand RemoveCommand { get; }

        public IDelegateCommand ResetCommand { get; }

        public IDelegateCommand SaveCommand { get; }

        public IAsyncCommand SendLogsCommand { get; }

        #endregion

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private ITagPrinterFactory TagPrinterFactory { get; }

        private IMapper Mapper { get; }

        protected override Task HandleLoadedAsync()
        {
            TagFormats = Dictionaries.GetItems<TagFormat>().ToReadOnlyObservableCollection();
            Prices = Dictionaries.GetItems<ProductPriceKind>()
                .Where(x => x.Real && !x.Configurator)
                .ToReadOnlyObservableCollection();

            Languages = Dictionaries.GetItems<Language>().ToReadOnlyObservableCollection();

            SelectedLanguage = Language.Ukrainian;

            return Task.WhenAll(RefreshWarehousesAsync(), RefreshEbalaAsync());

            async Task RefreshWarehousesAsync()
            {
                PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

                Warehouses = warehouses.Data
                    .Where(x => x.Active == 1
                                && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id)
                                && (x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.ShowCase.Id))
                    .OrderByDescending(x => x.Position)
                    .ToObservableCollection();

                SelectedWarehouse = Warehouses.FirstOrDefault();
            }

            async Task RefreshEbalaAsync()
            {
                warehouseTagFormatSettings = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseTagFormats(), true).GetPagedResultDataAsync();
            }
        }

        private async Task AddAsync()
        {
            try
            {
                NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(
                    new NomenclatureViewOptions(NomenclatureViewPriceContext.Client, Constants.RetailContractorId, NomenclatureViewSelectionMode.ByCheck, false),
                    this);

                if (nomenclatureViewModel.IsOk)
                {
                    int[] productIds = nomenclatureViewModel.GetSelectedItems().Select(x => x.Id).ToArray();

                    QueryProductTags request = new QueryProductTags(SelectedPrice.Id, productIds, null, null, null, true);

                    List<ProductTagDto> productTags = await WebClient.ExecuteApiRequestAsync(request);

                    AddTags(productTags);
                }
            }
            catch (Exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка. Не удалось добавить ценники для печати");
            }
        }

        private void AddTags(IEnumerable<ProductTagDto> productTags)
        {
            bool allSelected = Tags.Any() && SelectedTags.Count == Tags.Count;

            ProductTagViewItem[] viewItems = productTags.Select(x => Mapper.Map<ProductTagViewItem>(x)).ToArray();

            Logs = JsonConvert.SerializeObject(productTags.Where(x => x.PrintReasonData?.Any() == true).ToArray(), Formatting.None);

            Tags.AddRange(viewItems);

            if (allSelected)
            {
                SelectedTags.AddRange(viewItems);
            }
        }

        private bool CanAdd()
        {
            return SelectedPrice != null && SelectedWarehouse != null;
        }

        private bool CanFill()
        {
            return SelectedPrice != null && SelectedWarehouse != null;
        }

        private bool CanPrint()
        {
            return SelectedTags.Any(x => x.Price > 0);
        }

        private bool CanRemove()
        {
            return SelectedTags.Any();
        }

        private bool CanReset()
        {
            return Tags.Any();
        }

        private bool CanSave(TableView tableView)
        {
            return tableView != null && SelectedTags.Any();
        }

        private bool CanSendLogs()
        {
            return Tags.Any() && !string.IsNullOrWhiteSpace(Logs) && Logs.Length > 4;
        }

        private async Task FillAsync()
        {
            const int ShowLastValuesCount = 5;

            try
            {
                List<TagPrintInfoDto> tagPrintInfos = await WebClient.ExecuteApiRequestAsync(new QueryTagPrints(SelectedWarehouse.Id)).GetPagedResultDataAsync();
                List<DateTime> lastPrintDateTimeValues = tagPrintInfos
                    .OrderByDescending(x => x.Id)
                    .Take(ShowLastValuesCount)
                    .Select(x => x.DateTime)
                    .ToList();

                AutoFillOptionsViewModel viewModel = DialogDocumentManagerService.ShowView<AutoFillOptionsViewModel>("AutoFillOptionsView", lastPrintDateTimeValues, this);

                if (viewModel.IsOk && viewModel.FillAfterDateTime.HasValue)
                {
                    QueryProductTags request = new QueryProductTags(SelectedPrice.Id, null, SelectedWarehouse.Id, viewModel.FillAfterDateTime.Value, viewModel.TagFormatId, true);

                    List<ProductTagDto> productTags = await WebClient.ExecuteApiRequestAsync(request);

                    AddTags(productTags);
                }
            }
            catch (Exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка. Не удалось добавить ценники для печати");
            }
        }

        private async Task PrintAsync()
        {
            bool promoError = SelectedTags.Any(x => x.TagFormat == TagFormat.Promo && x.ActivePromo == false);

            if (promoError)
            {
                MessageFacadeService.ShowNotificationError("Ценники содержат неверный формат");
                return;
            }

            try
            {
                List<int> productIds = SelectedTags
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

                List<int> promoProductIds = SelectedTags
                    .Where(x => x.ActivePromo)
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

                List<ProductFeatureGroupsDto> featureDtos = await WebClient.ExecuteApiRequestAsync(new QueryProductFeatures(productIds));

                foreach (ProductFeatureGroupsDto featureDto in featureDtos)
                {
                    foreach (ProductFeatureGroupDto featureGroupDto in featureDto.AttributeGroups)
                    {
                        featureGroupDto.Attributes = featureGroupDto.Attributes.Where(x => x.PrintInTags).ToList();
                    }
                }

                IReadOnlyDictionary<int, IReadOnlyCollection<ProductFeatureGroupDto>> featuresDictionary = featureDtos.ToDictionary(
                    x => x.ProductId,
                    x => (IReadOnlyCollection<ProductFeatureGroupDto>)x.AttributeGroups);

                PagedResult<PromoDto> promosResult = await WebClient.ExecuteApiRequestAsync(new QueryPromos(new PromoFilteringItem(Subdivision.Telemart.Id, promoProductIds.ToArray(), true)));

                IEnumerable<IGrouping<int, TagPrintInfo>> tagPrintInfos = SelectedTags
                        .Select(x =>
                            new TagPrintInfo(
                                x.Id,
                                x.Name,
                                x.NameUkr,
                                x.Link,
                                x.LinkUkr,
                                x.Price,
                                x.PricePrev,
                                x.TagFormat.Id,
                                SelectedLanguage.Id,
                                x.BonusAmount,
                                featuresDictionary.GetValueOrDefault(x.Id, Array.Empty<ProductFeatureGroupDto>()),
                                x.TagFormat.Id == TagFormat.Promo.Id ? promosResult?.Data.FirstOrDefault(z => z.ProductIds.Contains(x.Id)) : null))
                        .GroupBy(x => x.FormatId);

                foreach (IGrouping<int, TagPrintInfo> grouping in tagPrintInfos)
                {
                    int tagFormatId = grouping.Key;

                    WarehouseTagFormatDto defaultSettings = warehouseTagFormatSettings.First(x => x.WarehouseId == null && x.TagFormatId == tagFormatId);

                    WarehouseTagFormatDto settings = warehouseTagFormatSettings
                        .FirstOrDefault(x => x.WarehouseId == SelectedWarehouse?.Id && x.TagFormatId == tagFormatId) ?? defaultSettings;

                    ITagPrinter tagPrinter = TagPrinterFactory.Create(tagFormatId, SelectedWarehouse.TagsHeader, settings.Width, settings.Height);

                    await tagPrinter.PrintAsync(grouping.Where(x => x.ProductPrice > 0).ToList());
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private void Remove()
        {
            if (SelectedTags.Count == Tags.Count)
            {
                Reset();
            }
            else
            {
                for (int i = SelectedTags.Count - 1; i >= 0; i--)
                {
                    Tags.Remove(SelectedTags[i]);
                }
            }
        }

        private void Reset()
        {
            Tags.Clear();
            Logs = string.Empty;
        }

        private void Save(TableView tableView)
        {
            bool promoError = SelectedTags.Any(x => x.TagFormat == TagFormat.Promo && x.ActivePromo == false);

            if (promoError)
            {
                MessageFacadeService.ShowNotificationError("Ценники содержат неверный формат");
                return;
            }

            string fileName = $"tags_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                e =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();
                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));
                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                },
                folderPath,
                fileName);
        }

        private async Task SendLogsAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter("Кратко опишите проблему", "Описание проблемы", isMultiline: true);

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var suffix = "tag_print";

            var filePath = Path.Combine(appDataPath, "telemart.client", "logs", $"log_{suffix}-{DateTime.Today:yyyyMMdd}.log");

            await File.WriteAllTextAsync(filePath, Logs);

            await _telemartClientLogger.SendLogsAsync(viewModel.Content, suffix);

            File.Delete(filePath);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Common.Utils.Import.ProductsFeatures;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.ParserFeature;
using Telemart.Client.Data.Requests.Features.Products.Content;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Directories.ProductsFeatures.YandexMarketFeaturesParser;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures
{
    public sealed class ProductFeaturesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private const string FeaturePropPrefix = "Feature";

        private readonly List<IAsyncCommand> asyncCommands;

        private int filterCounter;

        private Dictionary<int, ObservableCollection<FeatureValueViewItem>> featureValues;

        private List<ExpandoObject> allProducts;

        private Dictionary<int, FeatureDto> allFeatures;

        private ProductFeatureValueReplaceViewModel productFeatureValueReplaceViewModel;

        public ProductFeaturesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RefreshCommand = new AsyncCommand(RefreshAsync);
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            SaveCommand = new AsyncCommand(SaveAsync);
            SetFilterTypeCommand = new DelegateCommand<ProductsFeaturesFilterType>(SetFilterType);
            ExportCommand = new DelegateCommand<TableView>(Export, x => x != null);
            AutoCommand = new AsyncCommand(AutoAsync, () => Products?.Any() == true);
            ImportCommand = new DelegateCommand(Import);
            HandleTableViewLoadedCommand = new DelegateCommand<RoutedEventArgs>(HandleTableViewLoaded);
            ParseFromYandexMarketCommand = new DelegateCommand(ParseFromYandexMarket, () => Products?.Any() == true);
            HandleSelectionChangedCommand = new DelegateCommand(HandleSelectionChanged);
            ValueReplaceCommand = new DelegateCommand(ValueReplace, () => CurrentColumn?.FieldName.Contains(FeaturePropPrefix) == true && CurrentRow != null);
            LanguageChangedCommand = new DelegateCommand<Language>(LanguageChanged);
            CreateValueCommand = new DelegateCommand(CreateValue);

            Filter = new ProductFeaturesFilterViewModel(webClient, dictionaries, mapper);

            Mapper = mapper;

            asyncCommands = new List<IAsyncCommand>
            {
                RefreshCommand,
                SaveCommand
            };

            messenger.Register<EntityMessage<FeatureValueExDto>>(this, OnFeatureValueAction);
        }

        public ProductFeaturesViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ResetFilterCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand SetFilterTypeCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        public IDelegateCommand AutoCommand { get; }

        public IDelegateCommand ImportCommand { get; }

        public IDelegateCommand HandleTableViewLoadedCommand { get; }

        public IDelegateCommand ParseFromYandexMarketCommand { get; }

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public IDelegateCommand ValueReplaceCommand { get; }

        public IDelegateCommand LanguageChangedCommand { get; }

        public IDelegateCommand CreateValueCommand { get; }

        #endregion

        #region INPC

        public ProductFeaturesFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public ProductsFeaturesFilterType FilterType
        {
            get { return GetProperty(() => FilterType); }
            set { SetProperty(() => FilterType, value, () => FilterTypeChangedCallback(FilterType)); }
        }

        public ObservableRangeCollection<GridBandItem> Bands
        {
            get { return GetProperty(() => Bands); }
            private set { SetProperty(() => Bands, value); }
        }

        public ObservableRangeCollection<ExpandoObject> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ObservableRangeCollection<FormattingRule> FormattingRules
        {
            get { return GetProperty(() => FormattingRules); }
            private set { SetProperty(() => FormattingRules, value); }
        }

        public ReadOnlyObservableCollection<Language> Languages
        {
            get { return GetProperty(() => Languages); }
            private set { SetProperty(() => Languages, value); }
        }

        public Language SelectedLanguage
        {
            get { return GetProperty(() => SelectedLanguage); }
            private set { SetProperty(() => SelectedLanguage, value); }
        }

        public ColumnBase CurrentColumn
        {
            get { return GetProperty(() => CurrentColumn); }
            set { SetProperty(() => CurrentColumn, value); }
        }

        public IDictionary<string, object> CurrentRow
        {
            get { return GetProperty(() => CurrentRow); }
            set { SetProperty(() => CurrentRow, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public TableView TableView
        {
            get { return GetProperty(() => TableView); }
            set { SetProperty(() => TableView, value); }
        }

        #endregion

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        private IExcelImportSettingsEngine<IReadOnlyDictionary<string, object>, ProductsFeaturesExcelImportSettings> ExcelImportEngine { get; } = new ProductsFeaturesExcelImportEngine();

        private IMapper Mapper { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            if (asyncCommands.Any(x => x.IsExecuting))
            {
                return false;
            }

            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Control)
            {
                switch (msg.Key)
                {
                    case Key.S:
                        SaveCommand.Execute(null);
                        handled = true;
                        break;
                }
            }
            else if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.F:
                        FilterType = (ProductsFeaturesFilterType)(++filterCounter % 3);
                        handled = true;
                        break;
                    case Key.E:
                        ExportCommand.Execute(TableView);
                        handled = true;
                        break;
                    case Key.I:
                        ImportCommand.Execute(null);
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.Key)
                {
                    case Key.F5:
                        RefreshCommand.Execute(SelectedLanguage);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        public void ChangeValues(string fieldName, int fromValueId, int? toValueId)
        {
            int replacesCount = 0;

            foreach (ExpandoObject productRow in Products)
            {
                IDictionary<string, object> product = productRow;

                if (product.TryGetValue(fieldName, out object featureValueObject))
                {
                    if (featureValueObject is NullableIntPropertyValue intFeatureValue)
                    {
                        if (intFeatureValue.Value == fromValueId)
                        {
                            intFeatureValue.Value = toValueId;
                            replacesCount++;
                        }
                    }
                    else if (featureValueObject is CollectionPropertyValue collectionFeatureValue)
                    {
                        if (collectionFeatureValue.ChangeValue(fromValueId, toValueId))
                        {
                            replacesCount++;
                        }
                    }
                }
            }

            if (replacesCount > 0)
            {
                MessageFacadeService.ShowNotificationInfo($"Выполнено {replacesCount} {WordEndingHelper.GetWordByNumber(replacesCount, "замена", "замены", "замен")}");
                TableView.Grid.RefreshData();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Значения для замены не найдены");
            }
        }

        protected override Task HandleLoadedAsync()
        {
            LanguageChanged(Language.Russian);

            Languages = Dictionaries.GetItems<Language>().ToReadOnlyObservableCollection();

            return Filter.RefreshAsync();
        }

        private static int GetProductId(IDictionary<string, object> product)
        {
            NullableIntPropertyValue idProp = (NullableIntPropertyValue)product[nameof(ProductContentDto.Id)];
            return idProp.Value.Value;
        }

        private static string GetPropertyName(int featureId)
        {
            return $"{FeaturePropPrefix}{featureId}";
        }

        private static bool IsProductChanged(ExpandoObject product)
        {
            return GetTrackableValues(product).Any(v => v.IsChanged);
        }

        private static bool IsProductEmpty(ExpandoObject product)
        {
            return GetTrackableValues(product).All(v => v.IsEmpty());
        }

        private static IEnumerable<ITrackableValue> GetTrackableValues(ExpandoObject product)
        {
            return product.Where(pair => pair.Key.StartsWith(FeaturePropPrefix)).Select(pair => (ITrackableValue)pair.Value);
        }

        private static T GetPropertyOrDefault<T>(ExpandoObject product, string key, T defaultValue)
        {
            return product.GetValueOrDefault(key) is PropertyValueBase<T> property
                ? property.Value
                : defaultValue;
        }

        private static bool IsProductHasEmptyFeatureValues(ExpandoObject product)
        {
            return product.Any(x => x.Value is PropertyValueBase<int?> { Value: null });
        }

        private static IEnumerable<ExpandoObject> GetProducts(
           IReadOnlyCollection<ProductContentDto> data,
           IReadOnlyDictionary<int, FeatureDto> features)
        {
            HashSet<int> addedFeatures = new HashSet<int>();

            foreach (ProductContentDto product in data)
            {
                IDictionary<string, object> item = new ExpandoObject();

                item.Add(nameof(ProductContentDto.Name), new StringPropertyValue(product.Name));
                item.Add(nameof(ProductContentDto.Id), new NullableIntPropertyValue(product.Id));
                item.Add(nameof(ProductContentDto.CategoryId), new NullableIntPropertyValue(product.CategoryId));
                item.Add(nameof(ProductContentDto.YandexMarketId), new StringPropertyValue(product.YandexMarketId));

                foreach (var f in product.Features.GroupBy(x => x.FeatureId).Select(g => new { FeatureId = g.Key, FeatureValuesIds = g.Select(x => x.FeatureValueId) }))
                {
                    if (features.TryGetValue(f.FeatureId, out FeatureDto feature))
                    {
                        addedFeatures.Add(f.FeatureId);

                        string propertyName = GetPropertyName(f.FeatureId);

                        object propertyValue;

                        if (feature.Separator == null)
                        {
                            propertyValue = new NullableIntPropertyValue(f.FeatureValuesIds.First());
                        }
                        else
                        {
                            propertyValue = new CollectionPropertyValue(f.FeatureValuesIds.Cast<object>().ToList());
                        }

                        item.Add(propertyName, propertyValue);
                    }
                }

                foreach (FeatureDto notAddedFeature in features.Values.Where(f => !addedFeatures.Contains(f.Id)))
                {
                    string propertyName = GetPropertyName(notAddedFeature.Id);

                    if (notAddedFeature.Separator == null)
                    {
                        item.Add(propertyName, new NullableIntPropertyValue(null));
                    }
                    else
                    {
                        item.Add(propertyName, new CollectionPropertyValue(new List<object>()));
                    }
                }

                addedFeatures.Clear();

                yield return (ExpandoObject)item;
            }
        }

        private static IEnumerable<FormattingRule> GetFormattingRules(IEnumerable<string> properties)
        {
            const string ChangedToValueName = nameof(StringPropertyValue.IsChangedToValue);
            const string IsChangedFromEmptyName = nameof(StringPropertyValue.IsChangedFromEmpty);
            const string IsChangedToEmptyName = nameof(StringPropertyValue.IsChangedToEmpty);

            foreach (string x in properties.Where(x => x.StartsWith(FeaturePropPrefix)))
            {
                string fieldName = $"{GridColumnHelper.FieldNamePrefix}{x}";

                yield return new FormattingRule(fieldName, $"[{x}.{ChangedToValueName}] = True", false, PropertyChangeType.Changed);
                yield return new FormattingRule(fieldName, $"[{x}.{IsChangedFromEmptyName}] = True", false, PropertyChangeType.FromEmpty);
                yield return new FormattingRule(fieldName, $"[{x}.{IsChangedToEmptyName}] = True", false, PropertyChangeType.ToEmpty);
            }
        }

        private void HandleTableViewLoaded(RoutedEventArgs args)
        {
            TableView = args.Source as TableView;
        }

        private async Task AutoAsync()
        {
            bool anyNewFeaturesApplied = false;

            List<FeatureContractorParserSourceDto> featureSources = await WebClient.ExecuteApiRequestAsync(new QueryFeatureContractorParserSource(Filter.SelectedCategory.Id));

            Dictionary<int, int> featureContractorSourcesDictionary = featureSources
                .GroupBy(x => x.FeatureId)
                .ToDictionary(x => x.Key, x => x.OrderBy(z => z.Priority).First().ContractorId);

            List<ParserFeatureValueDto> parserFeatures = await WebClient.ExecuteApiRequestAsync(new QueryParserFeatureValues(new ParserAliasFilteringItem(Filter.SelectedCategory.Id)));

            Dictionary<int, ParserFeatureValueDto[]> parserFeatureProductValues = parserFeatures
                .GroupBy(x => x.FeatureId)
                .ToDictionary(x => x.Key, x => x
                    .Where(z => featureContractorSourcesDictionary.TryGetValue(x.Key, out int contractorId) && z.ContractorId == contractorId)
                    .ToArray());

            var productsWithEmptyFeatureValues = Products
                .Where(IsProductHasEmptyFeatureValues)
                .Select(x => new
                {
                    ProductId = GetPropertyOrDefault<int?>(x, nameof(ProductContentDto.Id), 0),
                    ProductName = GetPropertyOrDefault<string>(x, nameof(ProductContentDto.Name), null)
                })
                .Where(x => x.ProductId > 0 && !string.IsNullOrWhiteSpace(x.ProductName))
                .ToArray();

            foreach (var product in productsWithEmptyFeatureValues)
            {
                ExpandoObject expandProduct = Products.FirstOrDefault(x => ((StringPropertyValue)x.GetValueOrDefault(nameof(ProductContentDto.Name)))?.Value == product.ProductName);

                if (expandProduct == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, object> featureProperty in expandProduct.Where(x => x.Key.StartsWith(FeaturePropPrefix)))
                {
                    CollectionPropertyValue multiCellValue = null;
                    NullableIntPropertyValue cellValue = null;

                    if (!int.TryParse(featureProperty.Key.Replace(FeaturePropPrefix, string.Empty), out int featureId))
                    {
                        continue;
                    }

                    switch (featureProperty.Value)
                    {
                        case CollectionPropertyValue value:
                            multiCellValue = value;
                            break;
                        case NullableIntPropertyValue propertyValue:
                            cellValue = propertyValue;
                            break;
                    }

                    if ((multiCellValue is null || multiCellValue.Value != default) && (cellValue is null || cellValue.Value != default))
                    {
                        continue;
                    }

                    if (!featureValues.TryGetValue(featureId, out ObservableCollection<FeatureValueViewItem> availFeatureValues))
                    {
                        continue;
                    }

                    if (parserFeatureProductValues.TryGetValue(featureId, out ParserFeatureValueDto[] parserFeatureValues))
                    {
                        ParserFeatureValueDto[] newParserProductFeatureValues = parserFeatureValues.Where(x => x.ProductIds?.Contains(product.ProductId.Value) == true).ToArray();

                        foreach (ParserFeatureValueDto newParserFeatureValue in newParserProductFeatureValues)
                        {
                            FeatureValueViewItem availValue = availFeatureValues.FirstOrDefault(x => x.FeatureId == newParserFeatureValue.FeatureId);

                            if (availValue == null)
                            {
                                continue;
                            }

                            if (cellValue != null)
                            {
                                cellValue.Value = availValue.Id;
                                anyNewFeaturesApplied = true;
                            }

                            if (multiCellValue != null)
                            {
                                multiCellValue.Add(availValue.Id);
                                anyNewFeaturesApplied = true;
                            }
                        }
                    }
                }
            }

            if (anyNewFeaturesApplied)
            {
                MessageFacadeService.ShowNotificationInfo("Новые значения применены к таблице");
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Новых значений нет");
            }
        }

        private void Export(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            if (Filter.SelectedCategory == null)
            {
                return;
            }

            string fileName = $"Products_Features_{SelectedLanguage.ShortName}_{Filter.SelectedCategory.Name}_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ((FileDialogServiceBase)SaveFileDialogService).InitialDirectory = folderPath;

            SaveFileDialogService.DefaultFileName = $"{fileName}";

            if (SaveFileDialogService.ShowDialog())
            {
                IsLongOperationInProgress = true;

                List<ColumnBase> columnChooserColumns = new List<ColumnBase>(tableView.ColumnChooserColumns);

                foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                {
                    tableViewColumnChooserColumn.Visible = true;
                }

                string filePath = SaveFileDialogService.File.GetFullName();

                try
                {
                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));

                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                }
                catch (IOException exception) when (exception.Message.Contains("being used by another process"))
                {
                    MessageFacadeService.ShowNotificationError($"Файл {fileName} занят другим процессом");
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to export {FileName}", fileName);
                    MessageFacadeService.ShowNotificationError("Ошибка при экспорте");
                }

                foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                {
                    tableViewColumnChooserColumn.Visible = false;
                }

                IsLongOperationInProgress = false;
            }
        }

        private void ParseFromYandexMarket()
        {
            if (Products?.Any() != true)
            {
                return;
            }

            IReadOnlyDictionary<int, int> categoryHids = Filter.Categories.Where(x => x.YandexMarketHid != 0).ToDictionary(x => x.Id, x => x.YandexMarketHid);

            List<ProductFeaturesYandexMarketProductViewItem> products = Products.Select(x =>
                new ProductFeaturesYandexMarketProductViewItem
                {
                    Id = GetPropertyOrDefault<int?>(x, nameof(ProductContentDto.Id), 0) ?? 0,
                    EmptyFeatures = IsProductEmpty(x),
                    Name = GetPropertyOrDefault(x, nameof(ProductContentDto.Name), string.Empty),
                    YandexCategoryHid = categoryHids.GetValueOrDefault(GetPropertyOrDefault<int?>(x, nameof(ProductContentDto.CategoryId), 0) ?? 0),
                    YandexId = GetPropertyOrDefault(x, nameof(ProductContentDto.YandexMarketId), string.Empty)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.YandexId) && x.Id != 0 && x.YandexCategoryHid != 0)
                .ToList();

            Dictionary<string, string> featureMap = allFeatures.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.Mask))
                .ToDictionary(x => GetPropertyName(x.Id), y => y.Mask);

            ProductFeaturesYandexMarketParseParameter parserParameter = new ProductFeaturesYandexMarketParseParameter(products, featureMap);

            ProductFeaturesYandexMarketParseViewModel parserViewModel = DialogDocumentManagerService.ShowView<ProductFeaturesYandexMarketParseViewModel>(parserParameter, this);

            if (parserViewModel.IsOk && parserViewModel.ParsedProductFeatures?.Any() == true)
            {
                MapProducts(parserViewModel.ParsedProductFeatures, SelectedLanguage.Id);
            }
        }

        private void HandleSelectionChanged()
        {
            if (productFeatureValueReplaceViewModel != null)
            {
                if (CurrentColumn == null || CurrentRow == null)
                {
                    CloseReplaceDialog();
                }
                else
                {
                    string fieldName = CurrentColumn.FieldName.Replace(GridColumnHelper.FieldNamePrefix, string.Empty);

                    if (int.TryParse(fieldName.Replace(FeaturePropPrefix, string.Empty), out int featureId)
                        && CurrentRow.TryGetValue(fieldName, out object featureValueObject))
                    {
                        int? featureValueId = null;

                        switch (featureValueObject)
                        {
                            case NullableIntPropertyValue featureValue:
                                featureValueId = featureValue.Value;
                                break;
                            case CollectionPropertyValue collectionFeatureValue:
                                if (collectionFeatureValue.Value?.Count == 1)
                                {
                                    featureValueId = (int)collectionFeatureValue.Value[0];
                                }

                                break;
                        }

                        productFeatureValueReplaceViewModel.Refresh(CurrentColumn.Header.ToString(), fieldName, featureValueId, featureValues[featureId]);
                    }
                }
            }
        }

        private void CloseReplaceDialog()
        {
            if (productFeatureValueReplaceViewModel != null)
            {
                productFeatureValueReplaceViewModel.CancelCommand.Execute(null);
                productFeatureValueReplaceViewModel = null;
            }
        }

        private void ValueReplace()
        {
            productFeatureValueReplaceViewModel = NonModalDialogDocumentManagerService.ShowView<ProductFeatureValueReplaceViewModel>(null, this);
            HandleSelectionChanged();
        }

        private void LanguageChanged(Language language)
        {
            SelectedLanguage = language;

            featureValues?.SelectMany(x => x.Value).ForEach(x => x.SetLanguage(language.Id));

            RaisePropertiesChanged(nameof(SelectedLanguage));
        }

        private void CreateValue()
        {
            if (StringExtensions.TryParseInt32(CurrentColumn.FieldName, out int featureId)
                && allFeatures.TryGetValue(featureId, out FeatureDto feature))
            {
                DialogResult<FeatureValueExDto> dialogResult = DialogDocumentManagerService.ShowView<CreateProductFeatureValueViewModel, CreateProductFeatureValueParameter, FeatureValueExDto>(new CreateProductFeatureValueParameter(feature.Id, feature.Regex, feature.MultiLanguage, true), this);

                if (dialogResult.IsOk && CurrentRow.TryGetValue(GetPropertyName(featureId), out object value))
                {
                    if (value is NullableIntPropertyValue nullableIntPropertyValue)
                    {
                        nullableIntPropertyValue.Value = dialogResult.Result.Id;
                    }
                    else if (value is CollectionPropertyValue collectionPropertyValue)
                    {
                        collectionPropertyValue.Add(dialogResult.Result.Id);
                    }

                    TableView.HideEditor();
                }
            }
        }

        private IEnumerable<GridBandItem> GetBands(FeaturesMetadataDto meta)
        {
            foreach (GridBandItem band in GetPermanentBands())
            {
                yield return band;
            }

            foreach (FeatureGroupDto featureGroup in meta.Groups.OrderBy(x => x.Position))
            {
                GridBandItem featureGroupBand = new GridBandItem(featureGroup.Name.Trim());

                foreach (FeatureDto feature in featureGroup.Features.OrderBy(x => x.Position))
                {
                    if (!featureValues.TryGetValue(feature.Id, out ObservableCollection<FeatureValueViewItem> availValues))
                    {
                        availValues = new ObservableCollection<FeatureValueViewItem>();
                        featureValues.Add(feature.Id, availValues);
                    }

                    GridComboColumnItem column = new GridComboColumnItem(
                        GetPropertyName(feature.Id),
                        feature.Name.Trim(),
                        false,
                        true,
                        feature.Separator != null,
                        feature.Regex,
                        availValues,
                        feature.ManualInput);

                    featureGroupBand.Columns.Add(column);
                }

                yield return featureGroupBand;
            }

            static IEnumerable<GridBandItem> GetPermanentBands()
            {
                GridBandItem prodBand = new GridBandItem("Товар", true);

                prodBand.Columns.Add(new GridColumnItem(nameof(ProductContentDto.Name), "Название", false));
                prodBand.Columns.Add(new GridColumnItem(nameof(ProductContentDto.Id), "Код", true));

                yield return prodBand;
            }
        }

        private async Task RefreshAsync()
        {
            if (allProducts != null
                && allProducts.Any(IsProductChanged)
                && !MessageFacadeService.Confirm("Вы действительно хотите обновить данные?"))
            {
                return;
            }

            try
            {
                CloseReplaceDialog();

                await Filter.RefreshAsync();

                CategoryViewItem category = Filter.SelectedCategory;

                if (category == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите категорию");
                    return;
                }

                featureValues = null;
                allFeatures = null;
                allProducts = null;
                Products = null;
                FormattingRules = null;
                Bands = null;

                FilterType = ProductsFeaturesFilterType.All;

                int[] availTypes = Filter.SelectedAvailTypes?.Select(x => x.Id).ToArray();

                FeaturesResponse response = await WebClient.ExecuteApiRequestAsync(new QueryProductFeatures(category.Id, availTypes));

                featureValues = response.Meta.FeatureValues
                    .GroupBy(x => x.FeatureId)
                    .ToDictionary(g => g.Key, g => g.Select(y => Mapper.Map<FeatureValueViewItem>(y)).OrderBy(x => x.Value).ToObservableCollection());

                featureValues?.SelectMany(x => x.Value).ForEach(x => x.SetLanguage(SelectedLanguage.Id));

                allFeatures = response.Meta.Groups.SelectMany(x => x.Features).ToDictionary(x => x.Id);

                allProducts = GetProducts(response.Data, allFeatures).ToList();

                Products = allProducts.ToObservableRangeCollection();

                Bands = GetBands(response.Meta).ToObservableRangeCollection();

                IDictionary<string, object> product = allProducts.FirstOrDefault();

                if (product != null)
                {
                    FormattingRules = GetFormattingRules(product.Keys).ToObservableRangeCollection();
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh products features");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void Import()
        {
            bool isEdited = Products.Any(x => x.Any(y => ((ITrackableValue)y.Value).IsChanged));

            if (isEdited)
            {
                if (!MessageFacadeService.Confirm("Все текущие изменения могут быть отменены, продолжить?"))
                {
                    return;
                }
            }

            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return;
            }

            string fileName = OpenFileDialogService.GetFullFileName();

            List<Tuple<string, string>> columns = Bands
                .SelectMany(x => x.Columns
                .Select(y => new Tuple<string, string>(y.FieldName, $"{x.BandName}.{y.Header}")))
                .ToList();

            string[] productNames = Products.Select(x => ((StringPropertyValue)x.GetValueOrDefault(nameof(ProductContentDto.Name)))?.Value).ToArray();

            try
            {
                ExcelImportResult<IReadOnlyDictionary<string, object>> result = ExcelImportEngine.ImportFromXlsx(fileName, new ProductsFeaturesExcelImportSettings(columns, productNames));

                if (!result.IsSuccess)
                {
                    ShowValidationResultView("Ошибки при импорте файла", result.Errors);
                }
                else
                {
                    if (result.Errors.Any())
                    {
                        ShowValidationResultView("Предупреждения при импорте файла", result.Errors);
                    }

                    MapProducts(result.ResultItems, SelectedLanguage.Id);
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {fileName} занят другим процессом");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", fileName);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }
        }

        private void MapProducts(IReadOnlyCollection<IReadOnlyDictionary<string, object>> products, int languageId)
        {
            IsLongOperationInProgress = true;

            IReadOnlyCollection<FeatureValueViewItem> valuesToCreate = GetValuesToCreate(products, languageId);

            if (valuesToCreate.Any())
            {
                SizeableDialogDocumentManagerService.ShowView<FeatureValuesViewModel, FeatureValuesParameter, IReadOnlyCollection<FeatureValueViewItem>>(
                    new FeatureValuesParameter(Filter.SelectedCategory.Id, valuesToCreate), this);
            }

            List<string> errors = new List<string>();

            bool anyValueChanged = false;

            foreach (IReadOnlyDictionary<string, object> item in products)
            {
                string productName = item.GetValueOrDefault(nameof(ProductContentDto.Name), string.Empty).ToString();

                if (string.IsNullOrWhiteSpace(productName))
                {
                    continue;
                }

                ExpandoObject product = Products.FirstOrDefault(x => ((StringPropertyValue)x.GetValueOrDefault(nameof(ProductContentDto.Name)))?.Value == productName);

                if (product == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, object> featureProperty in product.Where(x => x.Key.StartsWith(FeaturePropPrefix)))
                {
                    if (!int.TryParse(featureProperty.Key.Replace(FeaturePropPrefix, string.Empty), out int featureId))
                    {
                        continue;
                    }

                    if (!featureValues.TryGetValue(featureId, out ObservableCollection<FeatureValueViewItem> availFeatureValues))
                    {
                        continue;
                    }

                    if (!allFeatures.TryGetValue(featureId, out FeatureDto feature))
                    {
                        continue;
                    }

                    if (!item.TryGetValue(featureProperty.Key, out object newValue))
                    {
                        continue;
                    }

                    if (feature.Separator == null)
                    {
                        NullableIntPropertyValue cellValue = (NullableIntPropertyValue)featureProperty.Value;

                        string newValueStr = newValue?.ToString();

                        if (!string.IsNullOrWhiteSpace(newValueStr))
                        {
                            FeatureValueViewItem availValue = availFeatureValues.FirstOrDefault(x => string.Equals(x.GetValue(languageId), newValueStr, StringComparison.Ordinal));

                            if (availValue == null)
                            {
                                errors.Add($"Значение {newValueStr} не найдено. Нужно создать значение вручную.");

                                continue;
                            }

                            cellValue.Value = availValue.Id;
                        }
                        else
                        {
                            cellValue.Value = null;
                        }

                        anyValueChanged |= cellValue.IsChanged;
                    }
                    else
                    {
                        CollectionPropertyValue cellValue = (CollectionPropertyValue)featureProperty.Value;

                        string newValueStr = newValue?.ToString();

                        if (!string.IsNullOrWhiteSpace(newValueStr))
                        {
                            foreach (string newValueStrPart in newValueStr.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                FeatureValueViewItem availValue = availFeatureValues.FirstOrDefault(x => string.Equals(x.GetValue(languageId), newValueStrPart, StringComparison.Ordinal));

                                if (availValue == null)
                                {
                                    errors.Add($"Значение {newValueStr} не найдено. Нужно создать значение вручную.");

                                    continue;
                                }

                                cellValue.Add(availValue.Id);
                            }
                        }
                        else
                        {
                            cellValue.Clear();
                        }

                        anyValueChanged |= cellValue.IsChanged;
                    }
                }
            }

            if (errors.Any())
            {
                ShowValidationResultView(
                    "Ошибки при добавлении значений",
                    errors.Select(x => new ValidationResultItem(x, true)).ToArray());
            }

            if (anyValueChanged)
            {
                FilterType = ProductsFeaturesFilterType.Changed;
            }

            IsLongOperationInProgress = false;
        }

        private IReadOnlyCollection<FeatureValueViewItem> GetValuesToCreate(IReadOnlyCollection<IReadOnlyDictionary<string, object>> products, int languageId)
        {
            List<FeatureValueViewItem> valuesToCreate = new List<FeatureValueViewItem>();

            foreach (IReadOnlyDictionary<string, object> product in products)
            {
                foreach (KeyValuePair<string, object> productValue in product)
                {
                    if (!int.TryParse(productValue.Key.Replace(FeaturePropPrefix, string.Empty), out int featureId))
                    {
                        continue;
                    }

                    if (!featureValues.TryGetValue(featureId, out ObservableCollection<FeatureValueViewItem> availFeatureValues))
                    {
                        continue;
                    }

                    if (!allFeatures.TryGetValue(featureId, out FeatureDto feature))
                    {
                        continue;
                    }

                    if (feature.ManualInput == false)
                    {
                        continue;
                    }

                    string newValueStr = productValue.Value?.ToString();

                    if (string.IsNullOrWhiteSpace(newValueStr))
                    {
                        continue;
                    }

                    if (feature.Separator == null)
                    {
                        if (!availFeatureValues.Any(x => string.Equals(x.GetValue(languageId), newValueStr, StringComparison.Ordinal))
                            && !valuesToCreate.Any(x => string.Equals(x.GetValue(languageId), newValueStr, StringComparison.Ordinal) && x.FeatureId == featureId))
                        {
                            valuesToCreate.Add(new FeatureValueViewItem(featureId, feature.Regex, newValueStr, languageId, feature.MultiLanguage));
                        }
                    }
                    else
                    {
                        foreach (string newValueStrPart in newValueStr.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!availFeatureValues.Any(x => string.Equals(x.GetValue(languageId), newValueStrPart, StringComparison.Ordinal))
                                && !valuesToCreate.Any(x => string.Equals(x.GetValue(languageId), newValueStrPart, StringComparison.Ordinal) && x.FeatureId == featureId))
                            {
                                valuesToCreate.Add(new FeatureValueViewItem(featureId, feature.Regex, newValueStrPart, languageId, feature.MultiLanguage));
                            }
                        }
                    }
                }
            }

            return valuesToCreate;
        }

        private async Task SaveAsync()
        {
            if (allProducts == null)
            {
                return;
            }

            ExpandoObject[] changed = allProducts.Where(IsProductChanged).ToArray();

            if (!changed.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            if (!MessageFacadeService.Confirm($"Будет сохранено {changed.Length} тов., продолжить?"))
            {
                return;
            }

            try
            {
                SaveFeatures gatewayRequest = new SaveFeatures(GetSaveDtos(changed).ToArray());

                Result<ProductContentDto[]> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings.Any())
                {
                    ShowValidationResultView(
                        "Предупреждения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }

                foreach (ExpandoObject o in changed)
                {
                    foreach (ITrackableValue trackableValue in GetTrackableValues(o).Where(x => x.IsChanged))
                    {
                        trackableValue.ApplyChanges();
                    }
                }

                Products = null;
                Products = allProducts.ToObservableRangeCollection();

                MessageFacadeService.ShowNotificationInfo($"Успешно сохранено {changed.Length} тов.");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save products features");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }

        private IEnumerable<ProductContentSaveDto> GetSaveDtos(IEnumerable<ExpandoObject> changedObjects)
        {
            foreach (ExpandoObject obj in changedObjects)
            {
                int productId = GetProductId(obj);

                yield return new ProductContentSaveDto(productId, GetFeatureValuesToSave(obj).ToArray());
            }
        }

        private IEnumerable<FeatureProductSaveDto> GetFeatureValuesToSave(ExpandoObject product)
        {
            foreach (KeyValuePair<string, object> pair in product.Where(x => x.Key.StartsWith(FeaturePropPrefix)))
            {
                int featureId = int.Parse(pair.Key.Replace(FeaturePropPrefix, string.Empty));

                FeatureDto feature = allFeatures[featureId];

                if (feature.Separator == null)
                {
                    NullableIntPropertyValue propValue = (NullableIntPropertyValue)pair.Value;

                    if (propValue.Value != null)
                    {
                        yield return new FeatureProductSaveDto(featureId, propValue.Value.Value);
                    }
                }
                else
                {
                    CollectionPropertyValue propValue = (CollectionPropertyValue)pair.Value;

                    if (propValue.Value != null)
                    {
                        foreach (int valueId in propValue.Value.Cast<int>())
                        {
                            yield return new FeatureProductSaveDto(featureId, valueId);
                        }
                    }
                }
            }
        }

        private void ResetFilter()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(SelectedLanguage);
        }

        private void SetFilterType(ProductsFeaturesFilterType type)
        {
            FilterType = type;
        }

        private void FilterTypeChangedCallback(ProductsFeaturesFilterType filterType)
        {
            if (allProducts == null)
            {
                return;
            }

            Products = filterType switch
            {
                ProductsFeaturesFilterType.All => allProducts.ToObservableRangeCollection(),
                ProductsFeaturesFilterType.Changed => allProducts.Where(IsProductChanged).ToObservableRangeCollection(),
                ProductsFeaturesFilterType.Empty => allProducts.Where(IsProductEmpty).ToObservableRangeCollection(),
                _ => throw new NotSupportedException()
            };
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }

        private void OnFeatureValueAction(EntityMessage<FeatureValueExDto> message)
        {
            if (featureValues == null)
            {
                return;
            }

            ObservableCollection<FeatureValueViewItem> values;

            switch (message.MessageType)
            {
                case MessageType.Added:

                    if (featureValues.TryGetValue(message.Entity.FeatureId, out values))
                    {
                        FeatureValueViewItem featureValue = Mapper.Map<FeatureValueViewItem>(message.Entity);

                        featureValue.SetLanguage(SelectedLanguage.Id);

                        values.Add(featureValue);
                        values = values.OrderBy(x => x.Value).ToObservableCollection();
                    }

                    break;

                case MessageType.Changed:

                    if (featureValues.TryGetValue(message.Entity.FeatureId, out values))
                    {
                        values.DoActionWithItem(x => x.Id == message.Entity.Id, x =>
                        {
                            Mapper.Map(message.Entity, x);
                            x.SetLanguage(SelectedLanguage.Id);
                        });
                    }

                    break;
            }
        }
    }
}
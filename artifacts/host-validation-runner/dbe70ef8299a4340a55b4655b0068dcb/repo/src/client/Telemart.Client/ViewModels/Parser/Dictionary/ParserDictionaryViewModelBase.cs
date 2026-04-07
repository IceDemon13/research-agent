using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Parser;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public abstract class ParserDictionaryViewModelBase : TelemartViewModelBase, ISupportHotkeys
    {
        private const string Separator = ", ";

        private readonly ObservableCollection<ParserAliasViewItemWrapper> aliasesPlain = new ObservableCollection<ParserAliasViewItemWrapper>();
        private readonly ObservableCollection<ParserAliasViewItemWrapper> aliasesWithJoinedCategories = new ObservableCollection<ParserAliasViewItemWrapper>();
        private readonly ObservableCollection<ParserAliasViewItemWrapper> aliasesWithJoinedCategoriesAndContractors = new ObservableCollection<ParserAliasViewItemWrapper>();
        private readonly ObservableCollection<ParserAliasViewItemWrapper> aliasesWithJoinedContractors = new ObservableCollection<ParserAliasViewItemWrapper>();
        private readonly List<IAsyncCommand> asyncCommands;

        private MultiValueGroupingMode multiValueGroupingMode = MultiValueGroupingMode.None;

        private List<CustomComboBoxItem> categoryFilterItems;
        private List<CustomComboBoxItem> contractorFilterItems;
        protected Dictionary<long, ParserAliasDto> aliasesDictionary;

        public ParserDictionaryViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            SaveCommand = new AsyncCommand(SaveAsync, IsAnyItemSelected);
            DeleteCommand = new AsyncCommand(DeleteAsync, IsAnyItemSelected);
            CompareAutoCommand = new AsyncCommand(CompareAutoAsync, IsAnyItemSelected);
            ExportCommand = new DelegateCommand(Export, IsAnyItemSelected);
            CompareManuallyCommand = new DelegateCommand(CompareManually, IsAnyItemSelected);
            SetNotAssociatedSateCommand = new AsyncCommand(SetNotAssociatedSateAsync, IsAnyItemSelected);
            SetPostponedStateCommand = new AsyncCommand(SetPostponedStateAsync, IsAnyItemSelected);
            SetIgnoredStateCommand = new AsyncCommand(SetIgnoredStateAsync, IsAnyItemSelected);
            HandleEndGroupingCommand = new DelegateCommand<GridControl>(HandleEndGrouping);
            HandleShownEditorCommand = new DelegateCommand<EditorEventArgs>(HandleShownEditor);
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);
            NavigateUriCommand = new DelegateCommand<string>(NavigateUri);
            Templates = new DelegateCommand(ParserSearchTemplates);

            asyncCommands = new List<IAsyncCommand>
            {
                RefreshCommand,
                SaveCommand,
                DeleteCommand,
                SetNotAssociatedSateCommand,
                SetPostponedStateCommand,
                SetIgnoredStateCommand
            };

            Filter = new ParserDictionaryFilter(Mapper);

            IsTemplateVisible = false;
            FeatureColumnVisible = false;
            CategoryColumnVisible = true;
        }

        public ParserDictionaryViewModelBase()
        {
        }

        #region INPC

        public ParserDictionaryFilter Filter { get; protected set; }

        public bool IsTemplateVisible { get; protected set; }

        public bool FeatureColumnVisible { get; protected set; }

        public bool CategoryColumnVisible
        {
            get { return GetProperty(() => CategoryColumnVisible); }
            set { SetProperty(() => CategoryColumnVisible, value); }
        }

        public bool AutoCompareByFeatures
        {
            get { return GetProperty(() => AutoCompareByFeatures); }
            protected set { SetProperty(() => AutoCompareByFeatures, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ObservableCollection<ParserAliasViewItemWrapper> ParserAliases
        {
            get { return GetProperty(() => ParserAliases); }
            private set { SetProperty(() => ParserAliases, value); }
        }

        public ParserAliasViewItemWrapper SelectedParserAlias
        {
            get { return GetProperty(() => SelectedParserAlias); }
            set { SetProperty(() => SelectedParserAlias, value); }
        }

        public ObservableCollection<ParserAliasViewItemWrapper> SelectedParserAliases { get; } = new ObservableCollection<ParserAliasViewItemWrapper>();

        #endregion

        #region Commands

        public IAsyncCommand CompareAutoCommand { get; }

        public IDelegateCommand CompareManuallyCommand { get; }

        public IAsyncCommand DeleteCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        public IDelegateCommand HandleEndGroupingCommand { get; }

        public IDelegateCommand HandleShownEditorCommand { get; }

        public IDelegateCommand NavigateUriCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ResetFilterCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IAsyncCommand SetIgnoredStateCommand { get; }

        public IAsyncCommand SetNotAssociatedSateCommand { get; }

        public IAsyncCommand SetPostponedStateCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        public IDelegateCommand Templates { get; }

        #endregion

        protected IMapper Mapper { get; }

        protected IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        protected IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

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
                    case Key.Delete:
                        DeleteCommand.Execute(null);
                        handled = true;
                        break;
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
                    case Key.C:
                        CompareManuallyCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.P:
                        SetPostponedStateCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.I:
                        SetIgnoredStateCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.E:
                        ExportCommand.Execute(null);
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.Key)
                {
                    case Key.F5:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.F6:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        break;
                    case Key.F9:
                        CompareAutoCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.Delete:
                        SetNotAssociatedSateCommand.Execute(null);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            await Filter.RefreshValuesAsync(WebClient);
            await base.HandleLoadedAsync();
        }

        protected virtual async Task CompareAutoAsync()
        {
            List<ParserAliasViewItemWrapper> toProcess = GetSelectedItemsToProcess()
                .Where(x => x.ComparsionResult == null)
                .ToList();

            if (!toProcess.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сопоставлять");
                return;
            }

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Сопоставление", toProcess.Count);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task<int> compareTask = Task<int>.Factory.StartNew(() =>
            {
                object syncRoot = new object();

                int processedItems = 0;

                Parallel.ForEach(
                    toProcess,
                    new ParallelOptions { CancellationToken = cancellationTokenSource.Token },
                    () => 0,
                    CompareAliasAuto,
                    localValue =>
                    {
                        lock (syncRoot)
                        {
                            processedItems += localValue;
                            progressViewModel.SetProcessedCount(processedItems);
                        }
                    });

                return processedItems;
            });

            DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            try
            {
                int items = await compareTask;
                MessageFacadeService.ShowNotificationInfo($"Сопоставлено {items} {DeclOfNum(items)}");
            }
            catch (OperationCanceledException)
            {
                MessageFacadeService.ShowNotificationWarning($"Отмена. Сопоставлено {progressViewModel.ProcessedCount} {DeclOfNum(progressViewModel.ProcessedCount)}");
            }
        }

        protected abstract string DeclOfNum(int number);

        protected abstract void CompareAliasAuto(ParserAliasViewItemWrapper alias);

        protected virtual async Task DeleteAsync()
        {
            try
            {
                long[] ids = GetSelectedItemsToProcess().Select(x => x.Id).ToArray();

                if (MessageFacadeService.Confirm($"Будет удалено {ids.Length.ToString(CultureInfo.InvariantCulture)} {DeclOfNum(ids.Length)}, продолжить?"))
                {
                    await DeleteParserAliasesAsync(ids);

                    HashSet<long> hashSet = new HashSet<long>(ids);

                    DeleteItems(aliasesPlain, hashSet);
                    DeleteItems(aliasesWithJoinedCategories, hashSet);
                    DeleteItems(aliasesWithJoinedContractors, hashSet);
                    DeleteItems(aliasesWithJoinedCategoriesAndContractors, hashSet);

                    MessageFacadeService.ShowNotificationInfo($"Удалено {ids.Length.ToString(CultureInfo.InvariantCulture)} {DeclOfNum(ids.Length)}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete parser aliases");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении товаров");
            }
        }

        protected abstract Task DeleteParserAliasesAsync(long[] ids);

        protected abstract bool ExportToExcel(string filePath, IReadOnlyCollection<ParserAliasDto> aliasesToExport);

        protected IReadOnlyCollection<ParserAliasViewItemWrapper> GetSelectedItemsToProcess()
        {
            if (!SelectedParserAliases.Any())
            {
                return Array.Empty<ParserAliasViewItemWrapper>();
            }

            HashSet<long> hashSet = new HashSet<long>(SelectedParserAliases.Select(x => x.Id));
            return aliasesWithJoinedCategoriesAndContractors.Where(x => hashSet.Contains(x.Id)).ToList();
        }

        protected abstract Task<IReadOnlyCollection<ParserAliasDto>> UpdateParserAliasesAsync(IReadOnlyCollection<ParserAliasSaveDto> parserAliases);

        protected abstract ParserAliasViewItem MapTransferObjectToViewItem(ParserAliasDto source);

        protected abstract Task RefreshInternalAsync();

        protected abstract Task<IReadOnlyCollection<ParserAliasDto>> GetParserAliasesAsync(ParserAliasFilteringItem filteringItem);

        private static void DeleteItems(ObservableCollection<ParserAliasViewItemWrapper> items, HashSet<long> idsToDelete)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (idsToDelete.Contains(items[i].Id))
                {
                    items.RemoveAt(i);
                }
            }
        }

        private static List<CustomComboBoxItem> GetCategoryFilterItems(IReadOnlyCollection<ParserAliasDto> aliases, IEnumerable<CategoryViewItem> categories)
        {
            List<CustomComboBoxItem> items = new List<CustomComboBoxItem>
            {
                new CustomComboBoxItem { DisplayValue = Constants.EmptyFilterItemDisplayValue, EditValue = null }
            };

            HashSet<int> hashSet = new HashSet<int>(aliases.SelectMany(x => x.CategoryIds).Distinct());

            IOrderedEnumerable<CustomComboBoxItem> comboBoxItems = categories
                .Where(x => hashSet.Contains(x.Id))
                .Select(x => new CustomComboBoxItem { DisplayValue = x.Name, EditValue = x.Name })
                .OrderBy(x => x.DisplayValue);

            items.AddRange(comboBoxItems);

            return items;
        }

        private static List<CustomComboBoxItem> GetContractorFilterItems(IReadOnlyCollection<ParserAliasDto> aliases, Dictionary<int, string> contractors)
        {
            List<CustomComboBoxItem> items = new List<CustomComboBoxItem>
            {
                new CustomComboBoxItem { DisplayValue = Constants.EmptyFilterItemDisplayValue, EditValue = null }
            };

            HashSet<int> hashSet = new HashSet<int>(aliases.SelectMany(x => x.ContractorProducts.Select(y => y.ContractorId)).Distinct());

            IOrderedEnumerable<CustomComboBoxItem> comboBoxItems = contractors
                .Where(x => hashSet.Contains(x.Key))
                .Select(x => new CustomComboBoxItem { DisplayValue = x.Value, EditValue = x.Value })
                .OrderBy(x => x.DisplayValue);

            items.AddRange(comboBoxItems);

            return items;
        }

        private static MultiValueGroupingMode GetGroupingMode(bool isGroupedByCategory, bool isGroupedByContractor)
        {
            MultiValueGroupingMode groupingMode;

            if (isGroupedByCategory && isGroupedByContractor)
            {
                groupingMode = MultiValueGroupingMode.CategoryAndContractor;
            }
            else if (isGroupedByCategory)
            {
                groupingMode = MultiValueGroupingMode.Category;
            }
            else if (isGroupedByContractor)
            {
                groupingMode = MultiValueGroupingMode.Contractor;
            }
            else
            {
                groupingMode = MultiValueGroupingMode.None;
            }

            return groupingMode;
        }

        private int CompareAliasAuto(ParserAliasViewItemWrapper alias, ParallelLoopState state, int localValue)
        {
            CompareAliasAuto(alias);
            return ++localValue;
        }

        private bool IsAnyItemSelected()
        {
            return SelectedParserAliases.Any();
        }

        private void CompareManually()
        {
            if (SelectedParserAliases.Count > 1)
            {
                MessageFacadeService.ShowNotificationWarning("Сопоставлять вручную можно только один товар");
                return;
            }

            ParserAliasViewItemWrapper parserAliasViewItem = SelectedParserAliases.First();

            try
            {
                NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(
                    new NomenclatureViewOptions(
                        NomenclatureViewPriceContext.Client,
                        Constants.TelemartContractorId,
                        NomenclatureViewSelectionMode.Single,
                        false,
                        excludeDiscounts: true),
                    this);

                if (nomenclatureViewModel.IsOk)
                {
                    NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                    ProductComparsionDto comparsionDto = new ProductComparsionDto
                    {
                        Id = product.Id,
                        Name = product.Name
                    };
                    ComparsionResult comparsionResult = new ComparsionResult(new[] { comparsionDto }, comparsionDto);
                    parserAliasViewItem.ComparsionResult = comparsionResult;
                    parserAliasViewItem.SelectedComparsionItem = new ComparsionItem(
                        (int)ParserAliasState.Associated,
                        comparsionResult.GuaranteedValue.Id,
                        comparsionResult.GuaranteedValue.Name,
                        ParserAliasState.Associated);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to compare alias manually");
                MessageFacadeService.ShowNotificationError("Ошибка при ручном сопоставлении");
            }
        }

        private void Export()
        {
            IReadOnlyCollection<ParserAliasViewItemWrapper> selectedItemsToProcess = GetSelectedItemsToProcess();

            if (selectedItemsToProcess.Any(x => x.Edited))
            {
                MessageFacadeService.ShowNotificationWarning("Экспорт недоступен. Сначала сохраните все изменения");
                return;
            }

            if (selectedItemsToProcess.Any(x => x.StateId != (int)ParserAliasState.NotAssosiated))
            {
                MessageFacadeService.ShowNotificationWarning("Экспорт доступен только для не сопоставленных товаров");
                return;
            }

            if (selectedItemsToProcess.Count > 100)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено экспортировать более 100 товаров");
                return;
            }

            string fileName = $"aliases_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                _ =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();

                    IReadOnlyCollection<ParserAliasDto> aliasesToExport = selectedItemsToProcess
                        .Select(x => aliasesDictionary[x.Id])
                        .ToList();

                    if (ExportToExcel(filePath, aliasesToExport))
                    {
                        MessageFacadeService.ShowNotificationInfo($"Экспортировано {selectedItemsToProcess.Count} {DeclOfNum(selectedItemsToProcess.Count)}");
                    }
                },
                folderPath,
                fileName);
        }

        private ObservableCollection<ParserAliasViewItemWrapper> GetParserAliasesItemsSource(MultiValueGroupingMode groupingMode)
        {
            ObservableCollection<ParserAliasViewItemWrapper> itemsSource;

            switch (groupingMode)
            {
                case MultiValueGroupingMode.None:
                    itemsSource = aliasesWithJoinedCategoriesAndContractors;
                    break;
                case MultiValueGroupingMode.Category:
                    itemsSource = aliasesWithJoinedContractors;
                    break;
                case MultiValueGroupingMode.Contractor:
                    itemsSource = aliasesWithJoinedCategories;
                    break;
                case MultiValueGroupingMode.CategoryAndContractor:
                    itemsSource = aliasesPlain;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return itemsSource;
        }

        private void HandleEndGrouping(GridControl gridControl)
        {
            GridColumn categoryColumn = gridControl.Columns[nameof(ParserAliasViewItemWrapper.Categories)];
            GridColumn contractorColumn = gridControl.Columns[nameof(ParserAliasViewItemWrapper.Contractors)];

            if (categoryColumn == null || contractorColumn == null)
            {
                return;
            }

            multiValueGroupingMode = GetGroupingMode(categoryColumn.IsGrouped, contractorColumn.IsGrouped);
            ParserAliases = GetParserAliasesItemsSource(multiValueGroupingMode);
        }

        private void HandleShownEditor(EditorEventArgs e)
        {
            if (SelectedParserAlias.ComparsionResult == null)
            {
                CompareAliasAuto(SelectedParserAlias);
            }

            ComboBoxEdit comboBoxEdit = (ComboBoxEdit)e.Editor;
            comboBoxEdit.IsPopupOpen = true;
        }

        private void NavigateUri(string uri)
        {
            try
            {
                ProcessHelper.Start(uri);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Can't navigate uri");
            }
        }

        private async Task RefreshAsync()
        {
            if (ParserAliases != null
                && ParserAliases.Any(x => x.Edited)
                && !MessageFacadeService.Confirm("Вы действительно хотите обновить данные?"))
            {
                return;
            }

            try
            {
                await Filter.RefreshValuesAsync(WebClient);

                Filter.RecordsCount = 0;
                ParserAliases = null;

                ParserAliasFilteringItem filteringItem = Filter.GetFilteringItem();

                if (filteringItem.Categories?.Any() != true)
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите категорию");
                    return;
                }

                await RefreshInternalAsync();

                IReadOnlyCollection<ParserAliasDto> parserAliases = await GetParserAliasesAsync(filteringItem);

                aliasesDictionary = parserAliases.ToDictionary(x => x.Id);

                aliasesPlain.Clear();
                aliasesWithJoinedCategories.Clear();
                aliasesWithJoinedContractors.Clear();
                aliasesWithJoinedCategoriesAndContractors.Clear();

                categoryFilterItems = GetCategoryFilterItems(aliasesDictionary.Values, Filter.Categories);
                contractorFilterItems = GetContractorFilterItems(aliasesDictionary.Values, Filter.ContractorsDictionary);

                Dictionary<int, string> categoriesDictionary = Filter.CategoriesDictionary;
                Dictionary<int, string> contractorsDictionary = Filter.ContractorsDictionary;

                foreach (ParserAliasDto source in aliasesDictionary.Values)
                {
                    ParserAliasViewItem viewItem = MapTransferObjectToViewItem(source);

                    string categories = string.Join(Separator, source.CategoryIds.Where(y => categoriesDictionary.ContainsKey(y)).Select(y => categoriesDictionary[y]));
                    string contractors = string.Join(Separator, source.ContractorProducts.Where(y => contractorsDictionary.ContainsKey(y.ContractorId)).Select(y => contractorsDictionary[y.ContractorId]));

                    List<ContractorLinkViewItem> contractorLinkViewItems = source.ContractorProducts
                        .Where(y => contractorsDictionary.ContainsKey(y.ContractorId))
                        .Select(y => new ContractorLinkViewItem(y.ContractorId, contractorsDictionary[y.ContractorId], y.Link))
                        .ToList();

                    aliasesWithJoinedCategoriesAndContractors.Add(new ParserAliasViewItemWrapper(viewItem, categories, contractors, contractorLinkViewItems));

                    foreach (int categoryId in source.CategoryIds)
                    {
                        if (categoriesDictionary.TryGetValue(categoryId, out string categoryName))
                        {
                            foreach (ParserContractorProductDto contractorProduct in source.ContractorProducts)
                            {
                                if (contractorsDictionary.TryGetValue(contractorProduct.ContractorId, out string contractorName))
                                {
                                    aliasesPlain.Add(new ParserAliasViewItemWrapper(viewItem, categoryName, contractorName, contractorLinkViewItems.Where(x => x.ContractorId == contractorProduct.ContractorId)));
                                }
                            }

                            aliasesWithJoinedContractors.Add(new ParserAliasViewItemWrapper(viewItem, categoryName, contractors, contractorLinkViewItems));
                        }
                    }

                    foreach (ParserContractorProductDto contractorProduct in source.ContractorProducts)
                    {
                        if (contractorsDictionary.TryGetValue(contractorProduct.ContractorId, out string contractorName))
                        {
                            aliasesWithJoinedCategories.Add(new ParserAliasViewItemWrapper(viewItem, categories, contractorName, contractorLinkViewItems.Where(x => x.ContractorId == contractorProduct.ContractorId)));
                        }
                    }
                }

                ParserAliases = GetParserAliasesItemsSource(multiValueGroupingMode);

                Filter.RecordsCount = aliasesDictionary.Count;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh parser aliases");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void ResetFilter()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private async Task SaveAsync()
        {
            try
            {
                List<ParserAliasSaveDto> toSave = GetSelectedItemsToProcess()
                    .Where(x => x.Edited)
                    .Select(x => new ParserAliasSaveDto { Id = x.Id, StateId = x.NewStateId, ProductId = x.NewProductId })
                    .ToList();

                if (!toSave.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                    return;
                }

                if (MessageFacadeService.Confirm($"Будет сохранено {toSave.Count.ToString(CultureInfo.InvariantCulture)} {DeclOfNum(toSave.Count)}, продолжить?"))
                {
                    IReadOnlyCollection<ParserAliasDto> fromServer = await UpdateParserAliasesAsync(toSave);

                    Dictionary<long, ParserAliasDto> fromServerDictionary = fromServer.ToDictionary(x => x.Id);

                    foreach (ParserAliasViewItemWrapper itemWrapper in ParserAliases)
                    {
                        if (fromServerDictionary.TryGetValue(itemWrapper.Id, out ParserAliasDto parserAliasDto))
                        {
                            itemWrapper.StateId = parserAliasDto.StateId;
                            itemWrapper.NewStateId = parserAliasDto.StateId;
                            itemWrapper.ProductId = parserAliasDto.ProductId;
                            itemWrapper.NewProductId = parserAliasDto.ProductId;
                        }
                    }

                    MessageFacadeService.ShowNotificationInfo($"Сохранено {fromServer.Count} {DeclOfNum(fromServer.Count)}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save parser aliases");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }

        private Task SetIgnoredStateAsync()
        {
            return SetStateAsync(GetSelectedItemsToProcess(), ComparsionItem.Ignored);
        }

        private Task SetNotAssociatedSateAsync()
        {
            return SetStateAsync(GetSelectedItemsToProcess(), ComparsionItem.NotAssosiated);
        }

        private Task SetPostponedStateAsync()
        {
            return SetStateAsync(GetSelectedItemsToProcess(), ComparsionItem.Postponed);
        }

        private Task<int> SetStateAsync(IReadOnlyCollection<ParserAliasViewItemWrapper> items, ComparsionItem comparsionItem)
        {
            Task<int> task;

            if (items.Count > 1)
            {
                if (MessageFacadeService.Confirm($"Будет изменено {items.Count.ToString(CultureInfo.InvariantCulture)} тов., продолжить?"))
                {
                    task = Task<int>.Factory.StartNew(
                        () =>
                        {
                            int processedItems = 0;

                            Parallel.ForEach(
                                items,
                                () => 0,
                                (item, state, localValue) =>
                                {
                                    item.SelectedComparsionItem = comparsionItem;
                                    return ++localValue;
                                },
                                localValue =>
                                {
                                    Interlocked.Add(ref processedItems, localValue);
                                });

                            return processedItems;
                        });
                }
                else
                {
                    task = Task.FromResult(0);
                }
            }
            else if (items.Count == 1)
            {
                items.First().SelectedComparsionItem = comparsionItem;
                task = Task.FromResult(1);
            }
            else
            {
                task = Task.FromResult(0);
            }

            return task;
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs e)
        {
            e.ComboBoxEdit.ItemsSource = e.Column.FieldName switch
            {
                nameof(ParserAliasViewItemWrapper.Categories) => categoryFilterItems,
                nameof(ParserAliasViewItemWrapper.Contractors) => contractorFilterItems,
                _ => e.ComboBoxEdit.ItemsSource
            };
        }

        private void ParserSearchTemplates()
        {
            SizeableDialogDocumentManagerService.ShowView<ParserSearchTemplatesViewModel>(null, this);
        }
    }
}
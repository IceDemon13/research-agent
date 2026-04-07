using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Products.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductDescription
{
    public class ProductDescriptionsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ProductDescriptionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IProductDescriptionDirectoryProcessor productDescriptionDirectoryProcessor,
            IPrintingSettingsStore printingSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DirectoryProcessor = productDescriptionDirectoryProcessor;
            PrintingSettingsStore = printingSettingsStore;

            AddCommand = new AsyncCommand<Language>(AddAsync);
            ClearCommand = new DelegateCommand(Clear);
            DeleteCommand = new DelegateCommand(Delete, () => SelectedProductDescription != null);
            SaveCommand = new AsyncCommand(SaveAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct, () => SelectedProductDescription != null);
        }

        public ProductDescriptionsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand AddCommand { get; }

        public IDelegateCommand ClearCommand { get; }

        public IDelegateCommand DeleteCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ProductDescriptionViewItem> ProductDescriptions
        {
            get { return GetProperty(() => ProductDescriptions); }
            private set { SetProperty(() => ProductDescriptions, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Languages
        {
            get { return GetProperty(() => Languages); }
            private set { SetProperty(() => Languages, value); }
        }

        public ProductDescriptionViewItem SelectedProductDescription
        {
            get { return GetProperty(() => SelectedProductDescription); }
            set { SetProperty(() => SelectedProductDescription, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferLocal);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IFolderBrowserDialogService FolderBrowserDialogService => GetService<IFolderBrowserDialogService>();

        private IProductDescriptionDirectoryProcessor DirectoryProcessor { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.Key)
            {
                case Key.Insert:
                    AddCommand.Execute(Language.Russian);
                    handled = true;
                    break;
                case Key.Delete:
                    DeleteCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            ProductDescriptions = new ObservableCollection<ProductDescriptionViewItem>();

            Languages = Dictionaries
                .GetItems<Language>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            return Task.CompletedTask;
        }

        private async Task AddAsync(Language language)
        {
            PrintingSettingsInfo loadFilesPath = await PrintingSettingsStore.LoadAsync();

            if (!string.IsNullOrEmpty(loadFilesPath.FilesPath))
            {
                FolderBrowserDialogService.StartPath = loadFilesPath.FilesPath;
            }

            if (!FolderBrowserDialogService.ShowDialog())
            {
                return;
            }

            string rootPath = FolderBrowserDialogService.ResultPath;

            if (!Directory.Exists(rootPath))
            {
                MessageFacadeService.ShowNotificationWarning("Папка не существует");
                return;
            }

            ProductDescriptionViewItem[] items = await DirectoryProcessor.ProcessAsync(rootPath);

            ProductDescriptionViewItem[] validItems = items.Where(x => x.Valid).ToArray();
            validItems.ForEach(x => x.LanguageId = language.Id);

            if (validItems.Any())
            {
                ValidationResultItem[] validationResults = items.SelectMany(x => x.ValidationResults).ToArray();

                if (!validationResults.Any() || ShowValidationResultView("Подтверждение", validationResults))
                {
                    QueryProductByNames gatewayRequest = new QueryProductByNames(
                        Constants.TelemartContractorId,
                        validItems.Select(x => x.FileNameWithoutExt).ToArray(),
                        Language.RussianId,
                        false,
                        tags: new[] { "product_descriptions" },
                        take: 1);

                    List<ProductSearchResponseDto> products = await WebClient.ExecuteCatalogApiRequestAsync(gatewayRequest);

                    int added = 0;

                    foreach (ProductDescriptionViewItem item in validItems)
                    {
                        ProductDto product = products.FirstOrDefault(x => string.Equals(x.Pattern, item.FileNameWithoutExt, StringComparison.Ordinal))?.Products
                            ?.OrderByDescending(x => x.Ratio)
                            .Select(x => x.Product)
                            .FirstOrDefault();

                        if (product != null)
                        {
                            item.ProductId = product.Id;
                            item.ProductName = product.Name;
                        }

                        ProductDescriptionViewItem existingItem = ProductDescriptions.FirstOrDefault(x => x.FilePath == item.FilePath && x.FileName == item.FileName);

                        if (existingItem == null)
                        {
                            ProductDescriptions.Add(item);
                        }
                        else
                        {
                            item.ProductId = existingItem.ProductId ?? item.ProductId;
                            item.ProductName = existingItem.ProductName ?? item.ProductName;

                            int index = ProductDescriptions.IndexOf(existingItem);
                            ProductDescriptions[index] = item;
                        }

                        added++;
                    }

                    MessageFacadeService.ShowNotificationInfo(Message(added));

                    SelectedProductDescription = ProductDescriptions.FirstOrDefault();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего добавлять");
            }

            static string Message(int added)
            {
                string action = WordEndingHelper.GetWordByNumber(added, new[] { "добавлен", "добавлено", "добавлено" });
                string word = WordEndingHelper.GetWordByNumber(added, new[] { "файл", "файла", "файлов" });

                return $"{action} {added} {word} с описанием".Transform(To.SentenceCase);
            }
        }

        private void Clear()
        {
            if (ProductDescriptions.Any())
            {
                if (MessageFacadeService.Confirm("Вы уверены, что хотите очистить все данные?"))
                {
                    ProductDescriptions.Clear();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего удалять");
            }
        }

        private void Delete()
        {
            if (MessageFacadeService.Confirm("Вы уверены, что хотите удалить файл с описанием?"))
            {
                ProductDescriptions.Remove(SelectedProductDescription);
            }
        }

        private async Task SaveAsync()
        {
            const int PartSize = 10;

            if (ProductDescriptions.Any(x => x.ProductId == null))
            {
                MessageFacadeService.ShowNotificationWarning("Не всем файлам проставлено соответствие");
                return;
            }

            ProductDescriptionViewItem[] toProcess = ProductDescriptions.Where(x => !x.IsProcessed || x.IsError).ToArray();

            if (!toProcess.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Обработка", toProcess.Length);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task<Task<int>> task = Task.Factory.StartNew(
                async () =>
                {
                    foreach (IReadOnlyCollection<ProductDescriptionViewItem> descriptions in toProcess.Section(PartSize))
                    {
                        string errorMessage = string.Empty;

                        try
                        {
                            await ProcessPartAsync(descriptions);
                            progressViewModel.SetProcessedCount(descriptions.Count);
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        }
                        catch (OperationCanceledException)
                        {
                            progressViewModel.CancelCommand.Execute(null);
                            errorMessage = $"Отмена. {Message(progressViewModel.ProcessedCount)}";
                            MessageFacadeService.ShowNotificationWarning(errorMessage);
                        }
                        catch (UnexpectedSatusException exception)
                        {
                            progressViewModel.CancelCommand.Execute(null);
                            errorMessage = "Ошибка при сохранении";
                            MessageFacadeService.ShowNotificationError(errorMessage);
                            ShowValidationResultView("Ошибки при сохранении", exception.GetErrorItems());
                        }
                        catch (UnexpectedErrorException exception)
                        {
                            progressViewModel.CancelCommand.Execute(null);
                            errorMessage = Resources.ServerConnectError;
                            Logger.LogError(exception, "Failed to save product descriptions");
                            ShowValidationResultView(errorMessage, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                        }
                        catch (Exception exception)
                        {
                            progressViewModel.CancelCommand.Execute(null);
                            errorMessage = "Ошибка при обработке данных";
                            Logger.LogError(exception, "Failed to prodess set product description");
                            MessageFacadeService.ShowNotificationError(errorMessage);
                        }

                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            foreach (ProductDescriptionViewItem productDescription in descriptions)
                            {
                                productDescription.ErrorMessage = errorMessage;
                            }

                            break;
                        }

                        progressViewModel.SetProcessedCount(descriptions.Count);
                    }

                    return toProcess.Count(x => x.IsSuccess);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.FromCurrentSynchronizationContext());

            DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            int processedCount = await task.Unwrap();
            MessageFacadeService.ShowNotificationInfo(Message(processedCount));

            static string Message(int n)
            {
                string action = WordEndingHelper.GetWordByNumber(n, new[] { "обработан", "обработано", "обработано" });
                string items = WordEndingHelper.GetWordByNumber(n, new[] { "товар", "товара", "товаров" });

                return $"{action} {n} {items}".Transform(To.SentenceCase);
            }
        }

        private async Task ProcessPartAsync(IReadOnlyCollection<ProductDescriptionViewItem> items)
        {
            ProductDescriptionSaveDto[] descriptions = items
                .Select(x => new ProductDescriptionSaveDto(x.ProductId.Value, x.LanguageId, x.Description))
                .ToArray();

            ProductDescriptionSaveResponse response = await WebClient.ExecuteApiRequestAsync(new SetProductDescription(descriptions));

            foreach (var x in items.Join(response.Results, d => d.ProductId, r => r.ProductId, (d, r) => new { Item = d, r.Error }))
            {
                x.Item.ErrorMessage = x.Error;
                x.Item.IsProcessed = true;
            }
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false,
                searchText: SelectedProductDescription.FileNameWithoutExt);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem result = nomenclatureViewModel.GetSelectedItems().First();

                SelectedProductDescription.ProductId = result.Id;
                SelectedProductDescription.ProductName = result.Name;
            }
        }
    }
}
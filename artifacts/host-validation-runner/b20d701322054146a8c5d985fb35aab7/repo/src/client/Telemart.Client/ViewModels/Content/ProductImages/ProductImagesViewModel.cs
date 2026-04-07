using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public sealed class ProductImagesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ProductImagesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IProductImagesDirectoryProcessor directoryProcessor,
            IPrintingSettingsStore printingSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DirectoryProcessor = directoryProcessor;
            AddCommand = new AsyncCommand(AddAsync);
            ClearCommand = new DelegateCommand(Clear);
            DeleteCommand = new DelegateCommand(Delete, () => SelectedProductDirectoryItem != null);
            SaveCommand = new AsyncCommand(SaveAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct, () => SelectedProductDirectoryItem != null);
            PrintingSettingsStore = printingSettingsStore;
        }

        public ProductImagesViewModel()
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

        public ObservableCollection<ProductImageDirectoryViewItem> ProductDirectoryItems
        {
            get { return GetProperty(() => ProductDirectoryItems); }
            private set { SetProperty(() => ProductDirectoryItems, value); }
        }

        public ProductImageDirectoryViewItem SelectedProductDirectoryItem
        {
            get { return GetProperty(() => SelectedProductDirectoryItem); }
            set { SetProperty(() => SelectedProductDirectoryItem, value); }
        }

        #endregion

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IFolderBrowserDialogService FolderBrowserDialogService => GetService<IFolderBrowserDialogService>();

        private IProductImagesDirectoryProcessor DirectoryProcessor { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.Key)
            {
                case Key.Insert:
                    AddCommand.Execute(null);
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
            ProductDirectoryItems = new ObservableCollection<ProductImageDirectoryViewItem>();

            return Task.CompletedTask;
        }

        private async Task AddAsync()
        {
            PrintingSettingsInfo loadImagesPath = await PrintingSettingsStore.LoadAsync();

            if (!string.IsNullOrEmpty(loadImagesPath.FilesPath))
            {
                FolderBrowserDialogService.StartPath = loadImagesPath.FilesPath;
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

            ProductImageDirectoryViewItem[] items = DirectoryProcessor.Process(rootPath);

            ProductImageDirectoryViewItem[] validItems = items.Where(x => x.Valid).ToArray();

            if (validItems.Any())
            {
                ValidationResultItem[] validationItems = items.SelectMany(x => x.GetValidationResults()).ToArray();

                if (validationItems.Length == 0 || ShowValidationResultView("Подтверждение", validationItems))
                {
                    QueryProductByNames gatewayRequest = new QueryProductByNames(
                        Constants.TelemartContractorId,
                        validItems.Select(x => x.DirectoryName).ToArray(),
                        Language.RussianId,
                        false,
                        tags: new[] { "product_images" },
                        take: 1);

                    List<ProductSearchResponseDto> products = await WebClient.ExecuteCatalogApiRequestAsync(gatewayRequest);

                    int added = 0;

                    foreach (ProductImageDirectoryViewItem item in validItems)
                    {
                        ProductDto product = products.FirstOrDefault(x => string.Equals(x.Pattern, item.DirectoryName, StringComparison.Ordinal))?.Products
                            ?.Select(x => x.Product)
                            .FirstOrDefault();

                        if (product != null)
                        {
                            item.ProductId = product.Id;
                            item.ProductName = product.Name;
                        }

                        ProductImageDirectoryViewItem existingItem = ProductDirectoryItems.FirstOrDefault(x => x.Path == item.Path && x.DirectoryName == item.DirectoryName);

                        if (existingItem == null)
                        {
                            ProductDirectoryItems.Add(item);
                        }
                        else
                        {
                            item.ProductId = existingItem.ProductId ?? item.ProductId;
                            item.ProductName = existingItem.ProductName ?? item.ProductName;

                            int index = ProductDirectoryItems.IndexOf(existingItem);
                            ProductDirectoryItems[index] = item;
                        }

                        added++;
                    }

                    MessageFacadeService.ShowNotificationInfo(Message(added));
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего добавлять");
            }

            static string Message(int added)
            {
                string action = WordEndingHelper.GetWordByNumber(added, new[] { "добавлена", "добавлено", "добавлено" });
                string word = WordEndingHelper.GetWordByNumber(added, new[] { "папка", "папки", "папок" });

                return $"{action} {added} {word} с фото".Transform(To.SentenceCase);
            }
        }

        private void Clear()
        {
            if (ProductDirectoryItems.Any())
            {
                if (MessageFacadeService.Confirm("Вы уверены, что хотите очистить все данные?"))
                {
                    ProductDirectoryItems.Clear();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего удалять");
            }
        }

        private void Delete()
        {
            if (MessageFacadeService.Confirm("Вы уверены, что хотите удалить папку?"))
            {
                ProductDirectoryItems.Remove(SelectedProductDirectoryItem);
            }
        }

        private async Task SaveAsync()
        {
            if (ProductDirectoryItems.Any(x => x.ProductId == null))
            {
                MessageFacadeService.ShowNotificationWarning("Не всем папкам проставлено соответствие");
                return;
            }

            ProductImageDirectoryViewItem[] toProcess = ProductDirectoryItems.Where(x => !x.IsProcessed).ToArray();

            if (toProcess.Length == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Обработка", toProcess.Length);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task<int> task = ProcessItemsAsync(toProcess, progressViewModel, cancellationTokenSource.Token);

            DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            try
            {
                int processedCount = await task;
                MessageFacadeService.ShowNotificationInfo(Message(processedCount));
            }
            catch (OperationCanceledException)
            {
                MessageFacadeService.ShowNotificationWarning($"Отмена. {Message(progressViewModel.ProcessedCount)}");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                ShowValidationResultView("Ошибки при сохранении", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save product images");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (IOException exception)
            {
                Logger.LogError(exception, "Failed to save product images");
                MessageFacadeService.ShowNotificationError($"Ошибка. {exception.Message}");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save product images");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке данных");
            }

            static string Message(int n)
            {
                string action = WordEndingHelper.GetWordByNumber(n, new[] { "обработан", "обработано", "обработано" });
                string items = WordEndingHelper.GetWordByNumber(n, new[] { "товар", "товара", "товаров" });

                return $"{action} {n} {items}".Transform(To.SentenceCase);
            }
        }

        private async Task<int> ProcessItemsAsync(
            ProductImageDirectoryViewItem[] toProcess,
            ProgressScreenViewModel progressViewModel,
            CancellationToken cancellationToken)
        {
            try
            {
                int processedItems = 0;

                foreach (ProductImageDirectoryViewItem item in toProcess)
                {
                    await ProcessItemAsync(item);
                    processedItems++;
                    progressViewModel.SetProcessedCount(processedItems);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return processedItems;
            }
            catch (Exception)
            {
                progressViewModel.CancelCommand.Execute(null);
                throw;
            }
        }

        private async Task ProcessItemAsync(ProductImageDirectoryViewItem item)
        {
            List<ImageDto> images = new List<ImageDto>();

            foreach (ProductImageFile x in item.Files.Where(x => x.Valid).OrderBy(x => x.FileNumber))
            {
                string filePath = Path.Combine(item.Path, item.DirectoryName, x.FileName);
                byte[] bytes = await FileHelper.ReadBytesAsync(filePath);
                images.Add(new ImageDto(x.FileName, bytes));
            }

            await WebClient.ExecuteApiRequestAsync(new UploadProductImages(item.ProductId.Value, images));

            item.IsProcessed = true;
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false,
                searchText: SelectedProductDirectoryItem.DirectoryName);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem result = nomenclatureViewModel.GetSelectedItems().First();

                SelectedProductDirectoryItem.ProductId = result.Id;
                SelectedProductDirectoryItem.ProductName = result.Name;
            }
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductVideos
{
    public class ProductVideosViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ProductVideosViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IExcelImportEngine<ProductVideoViewItem> excelImportEngine)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ExcelImportEngine = excelImportEngine;

            AddCommand = new AsyncCommand(AddAsync);
            DeleteCommand = new DelegateCommand(Delete, () => SelectedVideo != null);
            ClearCommand = new DelegateCommand(Clear);
            SaveCommand = new AsyncCommand(SaveAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct, () => SelectedVideo != null);

            Videos = new ObservableRangeCollection<ProductVideoViewItem>();
        }

        public IAsyncCommand AddCommand { get; }

        public IDelegateCommand DeleteCommand { get; }

        public IDelegateCommand ClearCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public ObservableRangeCollection<ProductVideoViewItem> Videos
        {
            get { return GetProperty(() => Videos); }
            private set { SetProperty(() => Videos, value); }
        }

        public ProductVideoViewItem SelectedVideo
        {
            get { return GetProperty(() => SelectedVideo); }
            set { SetProperty(() => SelectedVideo, value); }
        }

        private IExcelImportEngine<ProductVideoViewItem> ExcelImportEngine { get; }

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

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

        private async Task AddAsync()
        {
            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return;
            }

            string filePath = OpenFileDialogService.GetFullFileName();

            try
            {
                ExcelImportResult<ProductVideoViewItem> importResult = ExcelImportEngine.ImportFromXlsx(filePath);

                if (!importResult.IsSuccess)
                {
                    ShowValidationResultView("Ошибки при импорте файла", importResult.Errors);
                }
                else
                {
                    if (importResult.Errors.Any())
                    {
                        ShowValidationResultView("Предупреждения при импорте файла", importResult.Errors);
                    }

                    QueryProductByNames gatewayRequest = new QueryProductByNames(
                        Constants.TelemartContractorId,
                        importResult.ResultItems.Select(x => x.ExcelProductName).Distinct().ToArray(),
                        Language.RussianId,
                        false,
                        tags: new[] { "product_videos" },
                        take: 1);

                    List<ProductSearchResponseDto> reconizedProducts = await WebClient.ExecuteCatalogApiRequestAsync(gatewayRequest);

                    Dictionary<string, ProductDto> products = reconizedProducts
                        .Where(x => x.Products?.Any(y => y.Product != null) == true)
                        .GroupBy(x => x.Pattern)
                        .ToDictionary(x => x.Key, y => y.First().Products.OrderByDescending(z => z.Ratio).FirstOrDefault().Product);

                    int added = 0;

                    foreach (ProductVideoViewItem item in importResult.ResultItems)
                    {
                        ProductVideoViewItem video = Videos.FirstOrDefault(x => x.ExcelProductName.Equals(item.ExcelProductName, StringComparison.OrdinalIgnoreCase));

                        if (video == null)
                        {
                            if (products.TryGetValue(item.ExcelProductName, out ProductDto product))
                            {
                                item.ProductId = product.Id;
                                item.ProductName = product.Name;
                            }

                            Videos.Add(item);

                            added++;
                        }
                        else
                        {
                            foreach (string hash in item.Hashes)
                            {
                                video.Hashes.Add(hash);
                            }
                        }
                    }

                    if (added > 0)
                    {
                        string addStr = WordEndingHelper.GetWordByNumber(added, new[] { "Добавлен", "Добавлено", "Добавлено" });
                        string productStr = WordEndingHelper.GetWordByNumber(added, new[] { "товар", "товара", "товаров" });

                        MessageFacadeService.ShowNotificationInfo($"{addStr} {added} {productStr}");
                    }
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {filePath} занят другим процессом");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", filePath);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }
        }

        private void Delete()
        {
            Videos.Remove(SelectedVideo);
        }

        private void Clear()
        {
            if (Videos.Any())
            {
                if (MessageFacadeService.Confirm("Вы уверены, что хотите очистить все данные?"))
                {
                    Videos.Clear();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего удалять");
            }
        }

        private async Task SaveAsync()
        {
            if (!Videos.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            ProductVideoViewItem video = Videos.FirstOrDefault(x => x.ProductId == null);

            if (video != null)
            {
                MessageFacadeService.ShowNotificationWarning("Не всем товарам проставлено соответствие");
                SelectedVideo = video;
                return;
            }

            try
            {
                IReadOnlyCollection<ProductVideoDto> videos = Videos.
                    SelectMany(x => x.Hashes.Select(y => new ProductVideoDto { ProductId = x.ProductId.Value, Hash = y })).ToList();

                await WebClient.ExecuteApiRequestAsync(new SaveProductVideos(videos));

                MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                ShowValidationResultView("Ошибки при сохранении", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save product videos");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                Logger.LogError(exception, "Error while saving product videos");
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false,
                searchText: SelectedVideo.ExcelProductName);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem result = nomenclatureViewModel.GetSelectedItems().First();

                SelectedVideo.ProductId = result.Id;
                SelectedVideo.ProductName = result.Name;
            }
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }
    }
}
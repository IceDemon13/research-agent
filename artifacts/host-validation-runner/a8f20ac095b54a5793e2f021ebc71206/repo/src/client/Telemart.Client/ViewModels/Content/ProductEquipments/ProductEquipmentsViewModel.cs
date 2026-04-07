using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Telemart.Client.ViewModels.Content.ProductEquipments
{
    public class ProductEquipmentsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ProductEquipmentsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IExcelImportSettingsEngine<ProductEquipmentViewItem, ProductInfoType> excelImportEngine)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ExcelImportEngine = excelImportEngine;

            AddCommand = new AsyncCommand<ProductInfoType>(AddAsync);
            DeleteCommand = new DelegateCommand(Delete, () => SelectedEquipment != null);
            ClearCommand = new DelegateCommand(Clear);
            SaveCommand = new AsyncCommand(SaveAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct, () => SelectedEquipment != null);

            Equipments = new ObservableRangeCollection<ProductEquipmentViewItem>();
        }

        public IAsyncCommand AddCommand { get; }

        public IDelegateCommand DeleteCommand { get; }

        public IDelegateCommand ClearCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public ObservableRangeCollection<ProductEquipmentViewItem> Equipments
        {
            get { return GetProperty(() => Equipments); }
            private set { SetProperty(() => Equipments, value); }
        }

        public ProductEquipmentViewItem SelectedEquipment
        {
            get { return GetProperty(() => SelectedEquipment); }
            set { SetProperty(() => SelectedEquipment, value); }
        }

        public ReadOnlyObservableCollection<ProductInfoType> ProductInfoTypes
        {
            get { return GetProperty(() => ProductInfoTypes); }
            set { SetProperty(() => ProductInfoTypes, value); }
        }

        private IExcelImportSettingsEngine<ProductEquipmentViewItem, ProductInfoType> ExcelImportEngine { get; }

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.Key)
            {
                case Key.Delete:
                    DeleteCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            ProductInfoTypes = Dictionaries.GetItems<ProductInfoType>().ToReadOnlyObservableCollection();

            return Task.CompletedTask;
        }

        private async Task AddAsync(ProductInfoType type)
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
                ExcelImportResult<ProductEquipmentViewItem> importResult = ExcelImportEngine.ImportFromXlsx(filePath, type);

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

                    List<ProductEquipmentViewItem> newEquipments = (from er in importResult.ResultItems
                                                                    join e in Equipments on new { er.ExcelProductName, er.Type } equals new { e.ExcelProductName, e.Type } into eq
                                                                    from e in eq.DefaultIfEmpty()
                                                                    where e == null
                                                                    select er).ToList();

                    if (!newEquipments.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning("Нечего добавлять");
                        return;
                    }

                    QueryProductByNames gatewayRequest = new QueryProductByNames(
                        Constants.TelemartContractorId,
                        newEquipments.Select(x => x.ExcelProductName).Distinct().ToArray(),
                        Language.RussianId,
                        false,
                        tags: new[] { "product_equipments" },
                        take: 1);

                    List<ProductSearchResponseDto> recognizedProducts = await WebClient.ExecuteCatalogApiRequestAsync(gatewayRequest);

                    Dictionary<string, ProductDto> products = recognizedProducts
                        .Where(x => x.Products?.Any(y => y.Product != null) == true)
                        .ToDictionary(x => x.Pattern, y => y.Products.OrderByDescending(z => z.Ratio).First().Product);

                    int added = 0;

                    foreach (ProductEquipmentViewItem item in newEquipments)
                    {
                        if (products.TryGetValue(item.ExcelProductName, out ProductDto product))
                        {
                            item.ProductId = product.Id;
                            item.ProductName = product.Name;
                        }

                        Equipments.Add(item);

                        added++;
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
            Equipments.Remove(SelectedEquipment);
        }

        private void Clear()
        {
            if (Equipments.Any())
            {
                if (MessageFacadeService.Confirm("Вы уверены, что хотите очистить все данные?"))
                {
                    Equipments.Clear();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего удалять");
            }
        }

        private async Task SaveAsync()
        {
            if (!Equipments.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            ProductEquipmentViewItem equipment = Equipments.FirstOrDefault(x => x.ProductId == null);

            if (equipment != null)
            {
                MessageFacadeService.ShowNotificationWarning("Не всем товарам проставлено соответствие");
                SelectedEquipment = equipment;
                return;
            }

            try
            {
                IReadOnlyCollection<ProductEquipmentDto> equipments = Equipments.
                    Select(x => new ProductEquipmentDto
                    {
                        ProductId = x.ProductId.Value,
                        TypeId = x.Type.Id,
                        Equipment = x.Equipment,
                        EquipmentUkr = x.EquipmentUkr,
                        EquipmentEn = x.EquipmentEn
                    }).ToList();

                await WebClient.ExecuteApiRequestAsync(new SaveProductEquipments(equipments));

                MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                ShowValidationResultView("Ошибки при сохранении", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save product equipments");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                Logger.LogError(exception, "Error while saving product equipments");
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false,
                searchText: SelectedEquipment.ExcelProductName);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem result = nomenclatureViewModel.GetSelectedItems().First();

                SelectedEquipment.ProductId = result.Id;
                SelectedEquipment.ProductName = result.Name;
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
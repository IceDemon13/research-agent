using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssembledComputer;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class ServiceRequestNomenclatureSeriesViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, List<string>> accountingSystemSerials;
        private ScanSerialMode scanSerialMode = ScanSerialMode.Single;
        private TelemartEnumerableCompareHelper<ServiceRequestNomenclatureSeriesProductViewItem> compareHelper;

        public ServiceRequestNomenclatureSeriesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DeleteNewProductCommand = new DelegateCommand(DeleteNewProduct, () => SelectedNewProduct is not null && DiagnosticStage);
            AddNewProductCommand = new DelegateCommand(AddNewProduct, () => DiagnosticStage);
            EditSerialsCommand = new DelegateCommand(EditSerials, () => SelectedNewProduct is not null);
            ShowCurrentAssemblySerialsCommand = new DelegateCommand(ShowCurrentAssemblySerials, () => SelectedOldProduct is not null);
            ClearScannedQuantityCommand = new DelegateCommand(ClearScannedQuantity, () => !DiagnosticStage && SelectedNewProduct is not null);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;
        }

        public IDelegateCommand DeleteNewProductCommand { get; }

        public IDelegateCommand AddNewProductCommand { get; }

        public IDelegateCommand EditSerialsCommand { get; }

        public IDelegateCommand ShowCurrentAssemblySerialsCommand { get; }

        public IDelegateCommand ClearScannedQuantityCommand { get; }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        public string NomenclatureSeries
        {
            get { return GetProperty(() => NomenclatureSeries); }
            private set { SetProperty(() => NomenclatureSeries, value); }
        }

        public bool DiagnosticStage
        {
            get { return GetProperty(() => DiagnosticStage); }
            private set { SetProperty(() => DiagnosticStage, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            private set { SetProperty(() => ProductId, value); }
        }

        public ReadOnlyObservableCollection<AssembledComputerProductDto> OldProducts
        {
            get { return GetProperty(() => OldProducts); }
            private set { SetProperty(() => OldProducts, value); }
        }

        public ObservableCollection<ServiceRequestNomenclatureSeriesProductViewItem> NewProducts
        {
            get { return GetProperty(() => NewProducts); }
            set { SetProperty(() => NewProducts, value); }
        }

        public ServiceRequestNomenclatureSeriesProductViewItem SelectedNewProduct
        {
            get { return GetProperty(() => SelectedNewProduct); }
            set { SetProperty(() => SelectedNewProduct, value); }
        }

        public AssembledComputerProductDto SelectedOldProduct
        {
            get { return GetProperty(() => SelectedOldProduct); }
            set { SetProperty(() => SelectedOldProduct, value); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public bool AllowEditPrice
        {
            get { return GetProperty(() => AllowEditPrice); }
            private set { SetProperty(() => AllowEditPrice, value); }
        }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClose(e);
            }
        }

        public AssembledComputerSaveDto GetSaveDto()
        {
            AssembledComputerSaveDto saveDto = new AssembledComputerSaveDto()
            {
                ProductId = ProductId,
                Products = NewProducts.Select(x => new AssembledComputerProductSaveDto()
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    ScannedQuantity = x.ScannedQuantity,
                    SerialNumbers = x.SerialNumbers,
                    Price = x.Price
                }).ToArray()
            };

            return saveDto;
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceRequestNomenclatureSeriesParameter parameter = (ServiceRequestNomenclatureSeriesParameter)Parameter;

            ProductName = parameter.ProductName;
            NomenclatureSeries = parameter.NomenclatureSeries;
            DiagnosticStage = parameter.DiagnosticStage;
            AllowEditPrice = WebClient.IsOperationAllowed(BusinessOperation.EditPriceInServiceComputer);
            ProductId = parameter.ProductId;

            AssembledComputerDto assembledComputerDto = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputer(NomenclatureSeries));

            OldProducts = assembledComputerDto.Products.ToReadOnlyObservableCollection();

            if (DiagnosticStage)
            {
                NewProducts = assembledComputerDto.Products
                    .Select(x => new ServiceRequestNomenclatureSeriesProductViewItem()
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        DiagnosticStage = DiagnosticStage,
                        KeepSerial = x.SerialNumbers?.Any() == true,
                        KeepSerialOverridden = x.SerialNumbers?.Any() == true,
                        Quantity = x.Quantity,
                        Price = x.Price,
                        ProductFromOldAssembledComputer = OldProducts.Any(z => z.ProductId == x.ProductId),
                        ScannedQuantity = x.Quantity,
                        DefaultScannedQuantity = x.Quantity,
                        SerialNumbers = x.SerialNumbers?.Select(z => z.Sn).ToList() ?? new List<string>()
                    })
                    .ToObservableCollection();
            }
            else
            {
                AssembledComputerDto newAssembledComputerDto = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputer(NomenclatureSeries, parameter.ServiceRequestId));

                NewProducts = newAssembledComputerDto.Products
                    .Select(x => new ServiceRequestNomenclatureSeriesProductViewItem()
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        DiagnosticStage = DiagnosticStage,
                        KeepSerial = x.SerialNumbers?.Any() == true || (x.Quantity > x.ScannedQuantity && x.KeepSerial),
                        KeepSerialOverridden = x.SerialNumbers?.Any() == true || (x.Quantity > x.ScannedQuantity && x.KeepSerial),
                        Quantity = x.Quantity,
                        ScannedQuantity = x.ScannedQuantity,
                        Price = x.Price,
                        DefaultScannedQuantity = x.ScannedQuantity,
                        ProductFromOldAssembledComputer = OldProducts.Any(z => z.ProductId == x.ProductId),
                        SerialNumbers = x.SerialNumbers?.Select(z => z.Sn).ToList() ?? new List<string>()
                    })
                    .ToObservableCollection();
            }

            int[] productIds = NewProducts.Select(x => x.ProductId).ToArray();

            PagedResult<ProductAttributesDto> attributesResult = await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(productIds, true));

            RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), attributesResult?.Data);

            accountingSystemSerials = attributesResult?.Data.ToDictionary(x => x.ProductId, x => x.Serials);

            Title = "Создание новой комплектации";

            DateTime now = DateTime.Now;

            if (DiagnosticStage)
            {
                RecognizeBarcodeViewModel.RecognitionViewItems.Insert(0, new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Tip, "Необходимо выполнить сканирование товаров с дефектом для замены на такие же. Для замены на альтернативу удалите строчки с дефектными товарами и добавьте новые", now));
                MessageFacadeService.ShowNotificationInfo("Сканируйте поломанные товары,\nкоторый хотите извлечь или заменить");
            }
            else
            {
                RecognizeBarcodeViewModel.RecognitionViewItems.Insert(0, new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Tip, "Сканируйте товары, которые приехали под замену", now));
                MessageFacadeService.ShowNotificationInfo("Сканируйте товары, \n которые приехали под замену");
            }

            compareHelper = new TelemartEnumerableCompareHelper<ServiceRequestNomenclatureSeriesProductViewItem>(NewProducts);
        }

        protected override Task HandleOkAsync()
        {
            if (DiagnosticStage)
            {
                if (!compareHelper.IsChanged())
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                    return Task.CompletedTask;
                }

                if (!MessageFacadeService.Confirm("Вы уверены что все сломанные товары извлечены и добавлены новые в сборку?"))
                {
                    return Task.CompletedTask;
                }
            }

            if (!DiagnosticStage)
            {
                if (NewProducts.Any(x => x.ScannedQuantity < x.Quantity))
                {
                    MessageFacadeService.ShowNotificationError("Не все товары просканированы");
                    return Task.CompletedTask;
                }

                if (!MessageFacadeService.Confirm("Конфигурация ПК готова к выдаче/отправке?"))
                {
                    return Task.CompletedTask;
                }
            }

            CloseOk();
            return Task.CompletedTask;
        }

        private void AddNewProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem nomenclatureViewItem = nomenclatureViewModel.GetSelectedItems().First();

                if (NewProducts.Any(x => x.ProductId == nomenclatureViewItem.Id))
                {
                    MessageFacadeService.ShowNotificationWarning("Товар уже присутствует в новой сборке.\nУправляйте количеством в строке.");
                    return;
                }

                NewProducts.Add(new ServiceRequestNomenclatureSeriesProductViewItem()
                {
                    ProductId = nomenclatureViewItem.Id,
                    ProductName = nomenclatureViewItem.Name,
                    Quantity = nomenclatureViewItem.Quantity,
                    ScannedQuantity = 0,
                    DefaultScannedQuantity = 0,
                    Price = nomenclatureViewItem.Price,
                    SerialNumbers = new List<string>(),
                    DiagnosticStage = DiagnosticStage,
                    KeepSerial = nomenclatureViewItem.KeepSerial,
                    KeepSerialOverridden = nomenclatureViewItem.KeepSerial,
                    ProductFromOldAssembledComputer = OldProducts.Any(z => z.ProductId == nomenclatureViewItem.Id)
                });
            }
        }

        private void ShowCurrentAssemblySerials()
        {
            if (SelectedOldProduct.SerialNumbers?.Any() != true)
            {
                MessageFacadeService.ShowMessageBoxInfo("Товар не имеет серийных номеров");
                return;
            }

            DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(SelectedOldProduct.SerialNumbers.Select(x => x.Sn).ToList(), true), this);
        }

        private void EditSerials()
        {
            if (SelectedNewProduct.SerialNumbers?.Any() != true)
            {
                return;
            }

            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(SelectedNewProduct.SerialNumbers, DiagnosticStage), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            SelectedNewProduct.SerialNumbers = viewModel.SerialNumbers.ToList();
            SelectedNewProduct.Quantity = viewModel.SerialNumbers.Count;
        }

        private void ClearScannedQuantity()
        {
            SelectedNewProduct.ScannedQuantity = 0;
            SelectedNewProduct.SerialNumbers = new List<string>();
        }

        private void DeleteNewProduct()
        {
            if (SelectedNewProduct.ScannedQuantity > 0)
            {
                if (!MessageFacadeService.Confirm("Этот товар поломан, извлечен, и не требует замены?"))
                {
                    MessageFacadeService.ShowMessageBoxInfo("Если товар требует замены, тогда просканируйте его");
                    return;
                }
            }

            NewProducts.Remove(SelectedNewProduct);
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            IsRecognitionInProgress = false;

            switch (e.Result)
            {
                case RecognizeBarcodeResult.Found:

                    e.Message = DiagnosticStage ? RemoveBrokenProduct(e.Product, e.Quantity) : NewProductArrived(e.Product, e.Quantity);

                    break;
                case RecognizeBarcodeResult.FoundInSupplier:
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "ШК найден в БД, но не в текущей сборке");
                    break;
                case RecognizeBarcodeResult.NotFound:
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "ШК не найден в БД");
                    break;
            }
        }

        private RecognizeBarcodeMessage RemoveBrokenProduct(ProductAttributesDto product, int quantity)
        {
            ServiceRequestNomenclatureSeriesProductViewItem[] productViewItems = NewProducts.Where(x => x.ProductId == product.ProductId).ToArray();

            if (!productViewItems.Any())
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, $"Товара {product.FullName}\n нет в новой сборке");
            }

            if (productViewItems.All(x => !x.ProductFromOldAssembledComputer))
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, $"Товар {product.FullName}\nне присутствовал в старой сборке.\n Он не может быть извлечен.");
            }

            if (DiagnosticStage && productViewItems.All(x => x.ScannedQuantity == 0))
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, $"Товар {product.FullName}\n уже был извлечен");
            }

            ServiceRequestNomenclatureSeriesProductViewItem productViewItem = productViewItems.First(x => x.Quantity > 0);

            if (productViewItem.KeepSerialOverridden)
            {
                if (productViewItem.SerialNumbers?.Any() != true)
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "Такой товар отсутсвует в текущей сборке или был уже извлечен");
                }

                string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productViewItem.ProductId).ToArray();

                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    product.ProductId,
                    Array.Empty<string>(),
                    barcodes,
                    product.SerialNumberLength,
                    scanSerialMode,
                    validSerialNumbers: productViewItem.SerialNumbers,
                    errorForNotValidSerialNumbers: "SN не соответствует SN из товара сборки");

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    foreach (string serialNumberToRemove in viewModel.SerialNumbers)
                    {
                        productViewItem.SerialNumbers.Remove(serialNumberToRemove);
                    }

                    productViewItem.ScannedQuantity -= viewModel.SerialNumbers.Count;

                    scanSerialMode = viewModel.ScanMode;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                productViewItem.ScannedQuantity -= quantity;
            }

            return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Info, $"Товар {product.FullName} успешно извлечен из новой сборки");
        }

        private RecognizeBarcodeMessage NewProductArrived(ProductAttributesDto product, int quantity)
        {
            ServiceRequestNomenclatureSeriesProductViewItem[] productViewItems = NewProducts.Where(x => x.ProductId == product.ProductId).ToArray();

            if (!productViewItems.Any())
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товара {product.FullName} нет в новой сборке");
            }

            if (productViewItems.All(x => x.ScannedQuantity >= x.Quantity))
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товар {product.FullName} уже просканирован");
            }

            ServiceRequestNomenclatureSeriesProductViewItem productViewItem = productViewItems.First(x => x.ScannedQuantity < x.Quantity);

            if (productViewItem.KeepSerialOverridden)
            {
                string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productViewItem.ProductId).ToArray();

                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    product.ProductId,
                    productViewItem.SerialNumbers,
                    barcodes,
                    product.SerialNumberLength,
                    scanSerialMode,
                    accountingSystemSerials[productViewItem.ProductId]);

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    productViewItem.SerialNumbers.AddRange(viewModel.SerialNumbers);
                    productViewItem.ScannedQuantity = productViewItem.SerialNumbers.Count;

                    scanSerialMode = viewModel.ScanMode;
                }
            }
            else
            {
                productViewItem.ScannedQuantity += quantity;
            }

            return null;
        }

        private void RecognizeBarcodeViewModelOnFinishCommand(object sender, EventArgs e)
        {
            OkCommand.Execute(null);
        }
    }
}
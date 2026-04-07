using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.Dictionaries;
using Telemart.Common.ErrorHandling;
using ProductType = Telemart.Client.Dictionaries.ProductType;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public class CreateReasonNoProductModel : TelemartDialogViewModelBase
    {
        private const string SourceTextReason1 = "Ошибка цены. Цена в заказе: {0} грн, правильная цена: {1} грн";
        private const string SourceTextReason2 = "Товар закончился у поставщика {0}";

        private SetNoProductParameter _noProductParameter;
        private OrderDto _orderDto;

        public CreateReasonNoProductModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;

            SelectUnavailableProductCommand = new DelegateCommand(SelectProduct, () => EnabledUnavailableProduct);
            ClearUnavailableProductCommand = new DelegateCommand(ClearUnavailableProduct, () => UnavailableProductId.HasValue);
        }

        public string Content { get; private set; }

        public OrderSetUnavailableProductDto OrderSetUnavailableProductDto
        {
            get { return GetProperty(() => OrderSetUnavailableProductDto); }
            private set { SetProperty(() => OrderSetUnavailableProductDto, value); }
        }

        public ReadOnlyObservableCollection<NoProductReasonType> NoProductReasonTypes
        {
            get { return GetProperty(() => NoProductReasonTypes); }
            set { SetProperty(() => NoProductReasonTypes, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value); }
        }

        public int? ReasonTypeId
        {
            get { return GetProperty(() => ReasonTypeId); }
            set { SetProperty(() => ReasonTypeId, value, ChangeReasonTypeId); }
        }

        public decimal? CorrectPrice
        {
            get { return GetProperty(() => CorrectPrice); }
            set { SetProperty(() => CorrectPrice, value); }
        }

        public int? SelectSupplierId
        {
            get { return GetProperty(() => SelectSupplierId); }
            set { SetProperty(() => SelectSupplierId, value); }
        }

        public bool VisibleSuppliers
        {
            get { return GetProperty(() => VisibleSuppliers); }
            set { SetProperty(() => VisibleSuppliers, value, () => RaisePropertyChanged(nameof(SelectSupplierId))); }
        }

        public bool VisibleCorrectPrice
        {
            get { return GetProperty(() => VisibleCorrectPrice); }
            set { SetProperty(() => VisibleCorrectPrice, value, () => RaisePropertyChanged(nameof(CorrectPrice))); }
        }

        public bool VisibleLackAvailability
        {
            get { return GetProperty(() => VisibleLackAvailability); }
            set { SetProperty(() => VisibleLackAvailability, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public string UnavailableProductName
        {
            get { return GetProperty(() => UnavailableProductName); }
            set { SetProperty(() => UnavailableProductName, value); }
        }

        public int? UnavailableProductId
        {
            get { return GetProperty(() => UnavailableProductId); }
            set { SetProperty(() => UnavailableProductId, value, () => RaisePropertiesChanged(nameof(VisibleUnavailableProductPrice), nameof(UnavailableProductPrice))); }
        }

        public decimal? UnavailableProductPrice
        {
            get { return GetProperty(() => UnavailableProductPrice); }
            set { SetProperty(() => UnavailableProductPrice, value); }
        }

        public decimal OrderProductPrice
        {
            get { return GetProperty(() => OrderProductPrice); }
            set { SetProperty(() => OrderProductPrice, value); }
        }

        public int? UnavailableProductCurrencyId
        {
            get { return GetProperty(() => UnavailableProductCurrencyId); }
            set { SetProperty(() => UnavailableProductCurrencyId, value); }
        }

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public bool VisibleUnavailableProductPrice => UnavailableProductId > 0;

        public bool EnabledUnavailableProduct => _orderDto.ExternalPayments.Any(x => x.PaymentId == PaymentIds.MonobankId) != true;

        public IDelegateCommand SelectUnavailableProductCommand { get; }

        public IDelegateCommand ClearUnavailableProductCommand { get; }

        public override int MaxHeight => 300;

        public override int MinHeight => 220;

        public override int Height => 250;

        public override int MinWidth => 400;

        public override int Width => 400;

        public override int MaxWidth => 400;

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<CreateReasonNoProductModel> builder)
        {
            builder.Property(x => x.ReasonTypeId).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectSupplierId)
                .MatchesInstanceRule((x, y) => y.ReasonTypeId != NoProductReasonType.RanOutAtSupplier || (x != null && x > 0), () => "Выберите поставщика");

            builder.Property(x => x.CorrectPrice)
                .MatchesInstanceRule((x, y) => y.ReasonTypeId != NoProductReasonType.ErrorPrice || (x != null && x > 0), () => "Введите актуальную цену");

            builder.Property(x => x.Comment)
                .MatchesInstanceRule((x, y) => string.IsNullOrEmpty(x) || Regex.IsMatch(x, @"^[\s\S]{2,255}$"), () => "Допустимое количество символов от 2 до 255");

            builder.Property(x => x.UnavailableProductPrice)
                .MatchesInstanceRule((x, y) => y.UnavailableProductId == null || x > 0, () => "Цена альтернативного товара должна быть заполнена обязательно");
        }

        protected override async Task HandleLoadedAsync()
        {
            _noProductParameter = (SetNoProductParameter)Parameter;

            _orderDto = _noProductParameter.OrderDto;

            await Task.WhenAll(
                LoadSuppliersAsync(),
                LoadOrderProductForReasonAsync(),
                LoadProductInfoAsync(_noProductParameter.ProductId));

            NoProductReasonTypes = Dictionaries.GetItems<NoProductReasonType>()
                .Where(x => x.Id != NoProductReasonType.LackAvailability || VisibleLackAvailability)
                .ToReadOnlyObservableCollection();

            OrderProductPrice = _noProductParameter.ProductPriceOrder;

            Title = "Выберите причину отсутствия товара";

            RaisePropertyChanged(nameof(EnabledUnavailableProduct));
        }

        protected override async Task HandleOkAsync()
        {
            if (UnavailableProductId != null && !MessageFacadeService.Confirm("Исходный товар будет удален из заказа и заменен на альтернативный", "Вы уверены?"))
            {
                return;
            }

            _orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(_orderDto.Id));

            ValidationResultItem[] errorItems = CanSetUnavailableProduct()?.ToArray();

            if (errorItems?.Any() == true)
            {
                ShowValidationResultView("Ошибки при подмене текущего товара", errorItems);
                return;
            }

            if (await CheckCompatibilityAsync(_noProductParameter.OrderProductId, UnavailableProductId) == false)
            {
                return;
            }

            ContractorDto contractor = Suppliers.FirstOrDefault(x => x.Id == SelectSupplierId);

            NoProductCreateDto request = CreateNoProductRequest(contractor);

            Result<NoProductHistoryDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateNoProductHistory(request)),
                "сохранении в истории причины отсутсвия продукта",
                null,
                this,
                true,
                showNotification: false);

            if (result.IsSuccess)
            {
                SetContent(contractor?.Name);

                SetUnavailableProduct();

                CloseOk();
            }
        }

        private async Task LoadSuppliersAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Suppliers = contractors
                .Where(x => x.IsSupplier && !x.IsFolder)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadOrderProductForReasonAsync()
        {
            List<NoReasonOrderProductDto> orderProducts = await WebClient.ExecuteApiRequestAsync(new QueryOrderProductForReason(_orderDto.Id, _noProductParameter.ProductId));

            VisibleLackAvailability = orderProducts.Count > 0;
        }

        private async Task LoadProductInfoAsync(int productId)
        {
            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(Constants.TelemartContractorId, new[] { productId }));

            CategoryId = products.First().ParentCategoryId;
        }

        private void ChangeReasonTypeId()
        {
            VisibleSuppliers = ReasonTypeId == NoProductReasonType.RanOutAtSupplier;

            VisibleCorrectPrice = ReasonTypeId == NoProductReasonType.ErrorPrice;

            RaisePropertiesChanged(nameof(VisibleSuppliers), nameof(VisibleCorrectPrice));
        }

        private void SetContent(string contractorName)
        {
            switch (ReasonTypeId)
            {
                case NoProductReasonType.RanOutAtSupplier:
                    Content = string.Format(SourceTextReason2, contractorName);
                    break;
                case NoProductReasonType.ErrorPrice:
                    Content = string.Format(SourceTextReason1, _noProductParameter.ProductPriceOrder, CorrectPrice);
                    break;
                default:
                    Content = NoProductReasonTypes.FirstOrDefault(x => x.Id == ReasonTypeId)?.Name;
                    break;
            }

            if (!string.IsNullOrEmpty(Comment))
            {
                Content = $"{Content}: {Comment}";
            }
        }

        private void ClearUnavailableProduct()
        {
            UnavailableProductId = null;
            UnavailableProductName = null;
            UnavailableProductPrice = null;
            UnavailableProductCurrencyId = null;
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = null;

            OrderFolderDto orderFolderDto = GetOrderFolderDto(_orderDto, _noProductParameter.OrderProductId);

            if (orderFolderDto != null)
            {
                ProductQuantity[] productQuantities = _orderDto.Products
                    .Where(x => x.OrderFolderId == orderFolderDto.Id && x.Product.Id != _noProductParameter.ProductId)
                    .Select(y => new ProductQuantity(y.Product.Id, y.Quantity))
                    .ToArray();

                options = new NomenclatureViewOptions(
                    NomenclatureViewPriceContext.Client,
                    Constants.TelemartContractorId,
                    NomenclatureViewSelectionMode.Single,
                    true,
                    queryAdditionalServices: true,
                    queryPriceIn: true,
                    priceInIncludeReserve: true,
                    selectedCategoryId: CategoryId,
                    compatibleWithProducts: productQuantities);
            }
            else
            {
                options = new NomenclatureViewOptions(
                    NomenclatureViewPriceContext.Client,
                    Constants.TelemartContractorId,
                    NomenclatureViewSelectionMode.Single,
                    true,
                    selectedCategoryId: CategoryId);
            }

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                UnavailableProductId = product.Id;
                UnavailableProductName = product.GetLocalName(LocalizableNameType.Ukr);
                UnavailableProductPrice = product.Price;
                UnavailableProductCurrencyId = product.CurrencyId;
            }
            else
            {
                UnavailableProductId = null;
                UnavailableProductName = null;
                UnavailableProductPrice = null;
                UnavailableProductCurrencyId = null;
            }
        }

        private void SetUnavailableProduct()
        {
            if (UnavailableProductId.HasValue && UnavailableProductPrice.HasValue && UnavailableProductCurrencyId.HasValue)
            {
                OrderSetUnavailableProductDto = new OrderSetUnavailableProductDto(_noProductParameter.OrderProductId, UnavailableProductId.Value, UnavailableProductPrice.Value, UnavailableProductCurrencyId.Value);
            }
        }

        private NoProductCreateDto CreateNoProductRequest(ContractorDto contractor)
        {
            NoProductCreateDto request = new NoProductCreateDto(ReasonTypeId, _orderDto.Id, _noProductParameter.ProductId)
            {
                Comment = Comment
            };

            if (ReasonTypeId == NoProductReasonType.RanOutAtSupplier && contractor != null)
            {
                request.SupplierInfo(contractor.Id, contractor.Name);
            }

            if (ReasonTypeId == NoProductReasonType.ErrorPrice && CorrectPrice.HasValue)
            {
                request.SetPriceInfo(CorrectPrice.Value, _noProductParameter.ProductPriceOrder);
            }

            return request;
        }

        private async Task<bool> CheckCompatibilityAsync(int orderProductId, int? unavailableProductId)
        {
            OrderProductDto orderProductDto = _orderDto.Products.FirstOrDefault(x => x.Id == orderProductId);

            if (unavailableProductId.HasValue && orderProductDto?.AssemblyIncluded == true)
            {
                List<ProductQuantityDto> productQuantities = _orderDto.Products
                    .Where(x => x.Product.Id != Constants.AssemblyServiceProductId
                                && (!x.IsAdditionalService || x.Product.TypeId == ProductType.AccessoryId)
                                && x.AssemblyIncluded
                                && x.OrderFolderId == orderProductDto.OrderFolderId
                                && x.Product?.Id != orderProductDto.Product.Id)
                    .GroupBy(x => new { x.Product.Id, x.AssemblyQuantity!.Value })
                    .Select(x => new ProductQuantityDto(x.Key.Id, x.Key.Value))
                    .ToList();

                productQuantities.Add(new ProductQuantityDto(unavailableProductId.Value, orderProductDto.Quantity));

                int? assemblyProductId = _orderDto.Folders?.FirstOrDefault(x => x.Id == orderProductDto.OrderFolderId)?.ProductId;

                CheckCompatibilityResponse response = await WebClient.ExecuteCatalogApiRequestAsync(new CheckProductCompatibility(productQuantities, assemblyProductId));

                (int NotificationImageId, string Message)[] validations = response.GetValidationResults().ToArray();

                if (validations.Any())
                {
                    ShowValidationResultView("Валидация при проверке сборки", validations.Select(x => new ValidationResultItem(x.Message, x.NotificationImageId)));
                }
            }

            return true;
        }

        private OrderFolderDto GetOrderFolderDto(OrderDto orderDto, int noOrderProductId)
        {
            OrderProductDto orderProductDto = orderDto.Products.FirstOrDefault(x => x.Id == noOrderProductId);

            if (orderProductDto?.OrderFolderId.HasValue == true)
            {
                return orderDto.Folders.FirstOrDefault(
                    x => x.Id == orderProductDto.OrderFolderId
                         && (x.TypeId == OrderFolderType.AssemblyServiceId || x.TypeId == OrderFolderType.AssembledComputerRuleId));
            }

            return null;
        }

        private IEnumerable<ValidationResultItem> CanSetUnavailableProduct()
        {
            if (UnavailableProductId == null)
            {
                yield break;
            }

            if (_orderDto.EmployeeLockId != WebClient.AuthenticatedEmployee.Id)
            {
                yield return new ValidationResultItem("Заказ должен быть заблокирован вами", true);
            }

            if (_orderDto.Products?.Any(x => x.Product?.Id == _noProductParameter.ProductId) != true)
            {
                yield return new ValidationResultItem($"Не найден указанный товар в заказе №{_orderDto.Id}", true);
            }

            if (!UnavailableProductPrice.HasValue || UnavailableProductPrice <= 0)
            {
                yield return new ValidationResultItem("Цена должна быть больше нуля", true);
            }

            if (!EnabledUnavailableProduct)
            {
                yield return new ValidationResultItem("При способе оплаты монобанк нельзя подменять товары если отправили заявку", true);
            }

            if (Payment.IsCreditPayment(_orderDto.PaymentId) &&
                _orderDto.PaymentId != Payment.MonobankId
                && _noProductParameter.ProductPriceOrder != UnavailableProductPrice)
            {
                yield return new ValidationResultItem("В кредитных заказах цена альтернативного товара должна соответствовать товару, который отсутсвует", true);
            }

            if (Payment.IsCachlessPayment(_orderDto.PaymentId) && _noProductParameter.ProductPriceOrder != UnavailableProductPrice)
            {
                yield return new ValidationResultItem("В заказах со способ облаты Безнал с НДС и Безнал без НДС цена альтернативного товара должна соответствовать цене товара, который отсутсвует", true);
            }
        }
    }
}
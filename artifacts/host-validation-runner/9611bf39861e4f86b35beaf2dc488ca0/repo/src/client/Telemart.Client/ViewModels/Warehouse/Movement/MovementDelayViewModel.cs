using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class MovementDelayViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        private MovementDelayParameter _movementDelayParameter;

        public MovementDelayViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;
            ProductInformation = productInformationViewModel;
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ExpireReasons
        {
            get { return GetProperty(() => ExpireReasons); }
            private set { SetProperty(() => ExpireReasons, value); }
        }

        public int? ExpireReasonId
        {
            get { return GetProperty(() => ExpireReasonId); }
            set { SetProperty(() => ExpireReasonId, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public ReadOnlyObservableCollection<MovementDelayProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public MovementDelayProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, ProductChanged); }
        }

        public MovementDto Result { get; private set; }

        public static void BuildMetadata(MetadataBuilder<MovementDelayViewModel> builder)
        {
            builder.Property(x => x.ExpireReasonId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ExpireReasons = Dictionaries.GetItems<ExpireReasonType>()
                .Where(x => x.EntityId == Entity.MovementId)
                .Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            MovementDto movement = await WebClient.ExecuteApiRequestAsync(new QueryMovement(_movementDelayParameter.MovementId));

            Products = movement.MovementProducts
                .Where(x => x.QuantityOut > 0)
                .Select(x => _mapper.Map<MovementDelayProductViewItem>(x))
                .ToReadOnlyObservableCollection();

            List<ProductAlternativeDto> invoiceAlternatives = await WebClient.ExecuteApiRequestAsync(new QueryMovementAlternatives(_movementDelayParameter.MovementId));

            Dictionary<int, (int AlternativeType, int? WarehouseQuantityFree)> productAlternativeDictionary = invoiceAlternatives
                .ToDictionary(x => x.ProductId, x => (x.AlternativeType, x.WarehouseQuantityFree));

            IReadOnlyCollection<OrderProductDto> orderProductDtos = await LoadOrderProductsAsync(movement.OrderIds);

            foreach (MovementDelayProductViewItem product in Products)
            {
                (int AlternativeType, int? WarehouseQuantityFree) productAlternative = productAlternativeDictionary[product.ProductId];

                product.AlternativeId = productAlternative.AlternativeType;

                product.OrderQuantity = orderProductDtos.Count(x => x.MovementId == movement.Id && x.Product.Id == product.ProductId);

                product.RemoveSource = product.AlternativeId == (int)ProductAlternativeType.InStock && productAlternative.WarehouseQuantityFree > product.OrderQuantity
                    ? (short)SourceDecision.AutoSource
                    : (short)SourceDecision.SaveSource;
            }

            await base.HandleLoadedAsync();

            Title = "Принятие решения по товарам";
        }

        protected override async Task HandleOkAsync()
        {
            MovementDelayProductDto[] products = Products
                .Select(x => new MovementDelayProductDto(x.ProductId, x.RemoveSource))
                .ToArray();

            (await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new DelayMovement(
                        _movementDelayParameter.MovementId,
                        _movementDelayParameter.StateId == MovementState.NewId ? _movementDelayParameter.Date : null,
                        _movementDelayParameter.StateId == MovementState.LeftId ? _movementDelayParameter.Date : null,
                        _movementDelayParameter.StateId == MovementState.ReceivedId ? _movementDelayParameter.Date : null,
                        products,
                        ExpireReasonId!.Value,
                        _movementDelayParameter.WarehouseId)),
                    "смене даты прибытия",
                    "Дата прибытия изменена",
                    this,
                    true))
                .IfNotNull(x =>
                {
                    Result = x.Data;
                    IsOk = true;
                    Close();
                });
        }

        protected override void OnParameterChanged(object parameter)
        {
            _movementDelayParameter = (MovementDelayParameter)parameter;

            base.OnParameterChanged(parameter);
        }

        private void ProductChanged()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null && SelectedProduct.ProductId != 0)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, Currency.UahId);
            }
        }

        private async Task<IReadOnlyCollection<OrderProductDto>> LoadOrderProductsAsync(int[] orderIds)
        {
            if (orderIds?.Any() != true)
            {
                return Array.Empty<OrderProductDto>();
            }

            OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
            {
                OrderNumbers = string.Join(", ", orderIds)
            };

            List<OrderDto> listOrders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();

            return listOrders.SelectMany(x => x.Products).ToArray();
        }
    }
}
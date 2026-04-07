using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderConfirmViewModel : TelemartDialogViewModelBase, IOrderPaymentInfo
    {
        private OrderDto order;

        public OrderConfirmViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IOrderDeliveryCalculator deliveryCalculator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            DeliveryCalculator = deliveryCalculator;
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            OrderPaymentViewModel = new OrderPaymentInfoViewModel();

            Original = new LogisticsViewItem();
            Calculated = new LogisticsViewItem();

            HandleDeliveryChangedCommand = new DelegateCommand(RefreshPaymentItems);

            ValidationItems = new ObservableCollection<ValidationResultItem>();
        }

        public OrderConfirmViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleDeliveryChangedCommand { get; }

        #endregion

        public ReadOnlyObservableCollection<DeliveryPaymentMode> DeliveryPaymentModes
        {
            get { return GetProperty(() => DeliveryPaymentModes); }
            private set { SetProperty(() => DeliveryPaymentModes, value); }
        }

        #region Properties

        public LogisticsViewItem Original
        {
            get { return GetProperty(() => Original); }
            private set { SetProperty(() => Original, value); }
        }

        public LogisticsViewItem Calculated
        {
            get { return GetProperty(() => Calculated); }
            private set { SetProperty(() => Calculated, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            private set { SetProperty(() => Subdivision, value); }
        }

        public DateTime? DeliveryTime
        {
            get { return GetProperty(() => DeliveryTime); }
            set { SetProperty(() => DeliveryTime, value); }
        }

        public DateTime? DeliveryTimeTo
        {
            get { return GetProperty(() => DeliveryTimeTo); }
            set { SetProperty(() => DeliveryTimeTo, value); }
        }

        public int PackageDeliveryCost
        {
            get { return GetProperty(() => PackageDeliveryCost); }
            set { SetProperty(() => PackageDeliveryCost, value); }
        }

        public DeliveryPaymentMode PackageDeliveryPaid
        {
            get { return GetProperty(() => PackageDeliveryPaid); }
            set { SetProperty(() => PackageDeliveryPaid, value); }
        }

        public DateTime? OriginalReceiveTime
        {
            get { return GetProperty(() => OriginalReceiveTime); }
            set { SetProperty(() => OriginalReceiveTime, value); }
        }

        public DateTime? ReceiveTime
        {
            get { return GetProperty(() => ReceiveTime); }
            set { SetProperty(() => ReceiveTime, value); }
        }

        public DateTime ReceiveTimeMinValue
        {
            get { return GetProperty(() => ReceiveTimeMinValue); }
            private set { SetProperty(() => ReceiveTimeMinValue, value); }
        }

        public bool SendSms
        {
            get { return GetProperty(() => SendSms); }
            set { SetProperty(() => SendSms, value); }
        }

        public ReadOnlyObservableCollection<OrderProductViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            private set { SetProperty(() => OrderProducts, value); }
        }

        public ReadOnlyObservableCollection<OrderPaymentRecordViewItem> OrderPayments
        {
            get { return GetProperty(() => OrderPayments); }
            private set { SetProperty(() => OrderPayments, value); }
        }

        public OrderPaymentInfoViewModel OrderPaymentViewModel
        {
            get { return GetProperty(() => OrderPaymentViewModel); }
            private set { SetProperty(() => OrderPaymentViewModel, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            private set { SetProperty(() => ValidationItems, value, () => { RaisePropertyChanged(nameof(IsValidationItemsVisible)); }); }
        }

        public bool IsValidationItemsVisible => ValidationItems != null && ValidationItems.Any();

        #endregion

        #region IOrderPaymentInfo members

        public int Id => order.Id;

        int? IOrderPaymentInfo.PackageDeliveryCost => PackageDeliveryCost;

        int? IOrderPaymentInfo.MinPriceFreeDelivery { get; } = null;

        public decimal? MoneyBackAmount { get; set; }

        #endregion

        private IMapper Mapper { get; }

        private IOrderDeliveryCalculator DeliveryCalculator { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<OrderConfirmViewModel> builder)
        {
            builder.Property(x => x.DeliveryTime).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DeliveryTimeTo).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PackageDeliveryPaid).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PackageDeliveryCost);
        }

        IEnumerable<IOrderPaymentInfoProduct> IOrderPaymentInfo.GetOrderProducts() => OrderProducts;

        IEnumerable<IOrderPayment> IOrderPaymentInfo.GetOrderPayments() => OrderPayments;

        int IOrderPaymentInfo.GetBonusesQuantity() => order.Bonuses.Sum(x => x.Quantity);

        int IOrderPaymentInfo.GetBonusesToChargeQuantity() => order.Products.Sum(x => (x.BonusesToCharge ?? 0) * x.Quantity);

        protected override async Task HandleLoadedAsync()
        {
            order = (OrderDto)Parameter;

            DeliveryPaymentModes = new[] { DeliveryPaymentMode.We, DeliveryPaymentMode.Client }.ToReadOnlyObservableCollection();

            Subdivision = Dictionaries.GetItemById<Subdivision>(order.SubdivisionId);

            OrderViewItem orderViewItem = Mapper.Map<OrderViewItem>(order);

            OrderProducts = orderViewItem.Products.ToReadOnlyObservableCollection();

            OrderPayments = order.OrderPayments
                .Select(x => Mapper.Map<OrderPaymentRecordViewItem>(x))
                .ToReadOnlyObservableCollection();

            Original.DeliveryTime = order.DeliveryTime;
            Original.DeliveryTimeTo = order.DeliveryTimeTo;
            Original.PackageDeliveryCost = order.PackageDeliveryCost;
            Original.PackageDeliveryPaid = DeliveryPaymentMode.GetFromInt(order.PackageDeliveryPaid);

            OriginalReceiveTime = order.ReceiveTime;

            await Task.WhenAll(CalculateDeliveryTimeAsync());

            if (Original.DeliveryTime == null || Original.DeliveryTime.Value == Calculated.DeliveryTime)
            {
                DeliveryTime = Calculated.DeliveryTime;
            }

            if (Original.DeliveryTimeTo == null || Original.DeliveryTimeTo.Value == Calculated.DeliveryTimeTo)
            {
                DeliveryTimeTo = Calculated.DeliveryTimeTo;
            }

            PackageDeliveryCost = Calculated.PackageDeliveryCost = Original.PackageDeliveryCost;
            PackageDeliveryPaid = Calculated.PackageDeliveryPaid = DeliveryPaymentMode.We;

            ReceiveTimeMinValue = order.CreatedOn != DateTime.MinValue
                ? order.CreatedOn.AddDays(-1)
                : order.CreatedOn;
            ReceiveTime = order.ReceiveTime;
            MoneyBackAmount = order.MoneyBackAmount;

            RefreshPaymentItems();

            IReadOnlyCollection<ValidationResultItemDto> canConfirmValidationItems = await WebClient.ExecuteApiRequestAsync(new CanConfirmOrder(order.Id));

            ValidationItems.AddRange(canConfirmValidationItems.Select(x => new ValidationResultItem(x.Message, x.IsError)).ToObservableCollection());

            foreach (IGrouping<int, OrderProductViewItem> orderFolderProducts in OrderProducts.Where(x => x.OrderFolderId.HasValue).GroupBy(x => x.OrderFolderId.Value))
            {
                IReadOnlyCollection<ProductQuantityDto> productQuantities = orderFolderProducts
                    .Where(x => x.ProductTypeId != ProductType.AssemblyServiceId
                                && !x.IsGift
                                && !x.IsAdditionalService
                                && x.AssemblyIncluded)
                    .GroupBy(x => new { x.ProductId, x.AssemblyQuantity.Value })
                    .Select(x => new ProductQuantityDto(x.Key.ProductId, x.Key.Value))
                    .ToArray();

                CheckCompatibilityResponse response = await WebClient.ExecuteCatalogApiRequestAsync(new CheckProductCompatibility(productQuantities, null));

                ValidationItems.AddRange(response.GetValidationResults().Select(x => new ValidationResultItem(x.Message, NotificationImage.WarningId)));
            }

            if (ValidationItems.Any())
            {
                RaisePropertyChanged(nameof(IsValidationItemsVisible));
            }

            var orderSource = Dictionaries.GetItems<OrderSourceType>().FirstOrDefault(x => x.Id == order.OrderSourceId);

            if (orderSource?.CanContactCustomer == false)
            {
                SendSms = false;
            }
            else if (order.ConfirmedBy.HasValue)
            {
                SendSms = Original.DeliveryTime != Calculated.DeliveryTime || Original.DeliveryTimeTo != Calculated.DeliveryTimeTo;
            }
            else
            {
                SendSms = order.Options?.DontCall == true;
            }

            Title = $"Согласование заказа №{order.Id}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            DateTime deliveryTime = DeliveryTime.Value;
            DateTime deliveryTimeTo = DeliveryTimeTo.Value;

            if (deliveryTime > deliveryTimeTo)
            {
                MessageFacadeService.ShowNotificationWarning("Дата Х (От) не может быть больше Даты Х (До)");
                return;
            }

            if (ReceiveTime.HasValue)
            {
                if (ReceiveTime.Value < order.CreatedOn)
                {
                    MessageFacadeService.ShowNotificationWarning("Заберет меньше даты заказа");
                    return;
                }

                if (ReceiveTime.Value < deliveryTimeTo)
                {
                    MessageFacadeService.ShowNotificationWarning("Заберет меньше чем Дата Х");
                    return;
                }
            }

            try
            {
                ConfirmOrder gatewayRequest = new ConfirmOrder(
                    order.Id,
                    deliveryTime,
                    deliveryTimeTo,
                    ReceiveTime,
                    PackageDeliveryCost,
                    PackageDeliveryPaid.ToInt(),
                    SendSms);

                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Заказ подтвержден №{result.Data.Id} успешно");

                if (result.Warnings.Any())
                {
                    ShowValidationResultView("Предупреждения при подтверждении заказа", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }

                Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogDebug(exception, "{OrderId}. Unexpected status.", order.Id);
                ShowValidationResultView("Ошибки при подтверждении заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogDebug(exception, "{OrderId}. Unexpected error.", order.Id);
                ShowValidationResultView("Ошибки при подтверждении заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "{OrderId}. Failed to confirm order", order.Id);
                MessageFacadeService.ShowNotificationError("Ошибка при подтверждении заказа");
            }
        }

        protected override bool CanOk()
        {
            return ValidationItems == null || ValidationItems.Count == 0 || !ValidationItems.Any(x => x.IsError);
        }

        private void RefreshPaymentItems()
        {
            OrderPaymentViewModel.CalcPaymentInfo(this);
        }

        private async Task CalculateDeliveryTimeAsync()
        {
            OrderDeliveryTime result = await DeliveryCalculator.CalculateOrderDeliveryTimeAsync(
                order.Id,
                order.WarehouseId,
                order.AssemblyWarehouseId,
                order.BufferWarehouseId,
                order.AdditionalServiceWarehouseId,
                order.CarryId,
                order.SubdivisionId,
                Dictionaries.GetItemById<OrderStatus>(order.StateId),
                OrderProducts.Cast<IOrderProduct>().ToList(),
                order.Folders);

            foreach (KeyValuePair<int, DateTime> deliveryDate in result.OrderProductsDates)
            {
                OrderProductViewItem existingProduct = OrderProducts.FirstOrDefault(x => x.Id == deliveryDate.Key);

                if (existingProduct != null)
                {
                    existingProduct.DeliveryDateTime = deliveryDate.Value;
                }
            }

            Calculated.DeliveryTime = result.DeliveryTimeFrom;
            Calculated.DeliveryTimeTo = result.DeliveryTimeTo;

            if (result.AssemblyDates?.Any() == true)
            {
                ValidationItems.Add(new ValidationResultItem($"Даты сборок: {string.Join(", ", result.AssemblyDates.OrderBy(x => x).Select(x => x.ToString(DateFormattingRules.DateFormat)))}", false));

                RaisePropertyChanged(nameof(IsValidationItemsVisible));
            }

            if (result.AdditionalServiceDates?.Any() == true)
            {
                ValidationItems.Add(new ValidationResultItem($"Даты оказания услуг: {string.Join(", ", result.AdditionalServiceDates.OrderBy(x => x).Select(x => x.ToString(DateFormattingRules.DateFormat)))}", false));

                RaisePropertyChanged(nameof(IsValidationItemsVisible));
            }
        }

        public sealed class LogisticsViewItem : BindableBase
        {
            public DateTime? DeliveryTime
            {
                get { return GetProperty(() => DeliveryTime); }
                set { SetProperty(() => DeliveryTime, value); }
            }

            public DateTime? DeliveryTimeTo
            {
                get { return GetProperty(() => DeliveryTimeTo); }
                set { SetProperty(() => DeliveryTimeTo, value); }
            }

            public int PackageDeliveryCost
            {
                get { return GetProperty(() => PackageDeliveryCost); }
                set { SetProperty(() => PackageDeliveryCost, value); }
            }

            public DeliveryPaymentMode PackageDeliveryPaid
            {
                get { return GetProperty(() => PackageDeliveryPaid); }
                set { SetProperty(() => PackageDeliveryPaid, value); }
            }
        }
    }
}
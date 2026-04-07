using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderLogisticsViewModel : TelemartDialogViewModelBase, IOrderPaymentInfo
    {
        private OrderLogisticsParameter parameter;
        private int bonusesQuantity;
        private int bonusesToChargeQuantity;

        public OrderLogisticsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderDeliveryCalculator deliveryCalculator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DeliveryCalculator = deliveryCalculator;

            OrderPaymentViewModel = new OrderPaymentInfoViewModel();
        }

        public OrderLogisticsViewModel()
        {
        }

        public ReadOnlyObservableCollection<DeliveryPaymentMode> DeliveryPaymentModes
        {
            get { return GetProperty(() => DeliveryPaymentModes); }
            private set { SetProperty(() => DeliveryPaymentModes, value); }
        }

        #region Properties

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

        public string AssemblyDates
        {
            get { return GetProperty(() => AssemblyDates); }
            private set { SetProperty(() => AssemblyDates, value); }
        }

        public string AdditionalServiceDates
        {
            get { return GetProperty(() => AdditionalServiceDates); }
            private set { SetProperty(() => AdditionalServiceDates, value); }
        }

        public TimeSpan? TotalAdditionalServiceEstimate
        {
            get { return GetProperty(() => TotalAdditionalServiceEstimate); }
            private set { SetProperty(() => TotalAdditionalServiceEstimate, value); }
        }

        public string WarehouseQuotaUsage
        {
            get { return GetProperty(() => WarehouseQuotaUsage); }
            private set { SetProperty(() => WarehouseQuotaUsage, value); }
        }

        public DeliveryPaymentMode PackageDeliveryPaid
        {
            get { return GetProperty(() => PackageDeliveryPaid); }
            set { SetProperty(() => PackageDeliveryPaid, value); }
        }

        public OrderPaymentInfoViewModel OrderPaymentViewModel
        {
            get { return GetProperty(() => OrderPaymentViewModel); }
            private set { SetProperty(() => OrderPaymentViewModel, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 480;

        public override int MinWidth => 640;

        public override int Width => 800;

        #endregion

        #region IOrderPaymentInfo members

        public int Id => parameter.Id;

        int? IOrderPaymentInfo.PackageDeliveryCost => PackageDeliveryCost;

        int? IOrderPaymentInfo.MinPriceFreeDelivery { get; } = null;

        public decimal? MoneyBackAmount { get; set; }

        private IOrderDeliveryCalculator DeliveryCalculator { get; }

        IEnumerable<IOrderPaymentInfoProduct> IOrderPaymentInfo.GetOrderProducts() => OrderProducts;

        IEnumerable<IOrderPayment> IOrderPaymentInfo.GetOrderPayments() => OrderPayments;

        int IOrderPaymentInfo.GetBonusesQuantity() => bonusesQuantity;

        int IOrderPaymentInfo.GetBonusesToChargeQuantity() => bonusesToChargeQuantity;

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            parameter = (OrderLogisticsParameter)Parameter;

            DeliveryPaymentModes = new[] { DeliveryPaymentMode.We, DeliveryPaymentMode.Client }.ToReadOnlyObservableCollection();

            Subdivision = Dictionaries.GetItemById<Subdivision>(parameter.SubdivisionId);
            OrderProducts = parameter.OrderProducts.ToReadOnlyObservableCollection();
            OrderPayments = parameter.OrderPayments.ToReadOnlyObservableCollection();
            MoneyBackAmount = parameter.MoneyBackAmount;
            bonusesQuantity = parameter.BonusesQuantity;
            bonusesToChargeQuantity = parameter.BonusesToChargeQuantity;

            await Task.WhenAll(CalculateDeliveryTimeAsync(), CalculateDeliveryCostAsync());

            RefreshPaymentItems();

            Title = "Логистика";
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void RefreshPaymentItems()
        {
            OrderPaymentViewModel.CalcPaymentInfo(this);
        }

        private async Task CalculateDeliveryTimeAsync()
        {
            OrderDeliveryTime result = await DeliveryCalculator.CalculateOrderDeliveryTimeAsync(
                parameter.Id,
                parameter.WarehouseId,
                parameter.AssemblyWarehouseId,
                parameter.BufferWarehouseId,
                parameter.AdditionalServiceWarehouseId,
                parameter.CarryId,
                parameter.SubdivisionId,
                Dictionaries.GetItemById<OrderStatus>(parameter.StateId),
                OrderProducts.Cast<IOrderProduct>().ToList(),
                parameter.Folders);

            foreach (KeyValuePair<int, DateTime> deliveryDate in result.OrderProductsDates)
            {
                OrderProductViewItem existingProduct = OrderProducts.FirstOrDefault(x => x.Id == deliveryDate.Key);

                if (existingProduct == null)
                {
                    continue;
                }

                existingProduct.DeliveryDateTime = deliveryDate.Value;
            }

            if (result.AssemblyDates?.Any() == true)
            {
                AssemblyDates = string.Join(", ", result.AssemblyDates.OrderBy(x => x).Select(x => x.ToString(DateFormattingRules.DateFormat)));
            }

            if (result.AdditionalServiceDates?.Any() == true)
            {
                AdditionalServiceDates = string.Join(", ", result.AdditionalServiceDates.OrderBy(x => x).Select(x => x.ToString(DateFormattingRules.DateFormat)));
            }

            if (result.AdditionalServiceQuotas?.Any() == true)
            {
                WarehouseQuotaUsage = string.Join(", ", result.AdditionalServiceQuotas
                    .OrderBy(x => x.Date)
                    .Select(x => $"{x.Date:d}: {(x.Used ?? TimeSpan.Zero).ToString(@"hh\:mm")}/{x.Total?.ToString(@"hh\:mm") ?? "∞"}"));
            }

            DeliveryTime = result.DeliveryTimeFrom;
            DeliveryTimeTo = result.DeliveryTimeTo;
            TotalAdditionalServiceEstimate = result.TotalAdditionalServiceEstimate;
        }

        private async Task CalculateDeliveryCostAsync()
        {
            if (parameter.FreeDelivery)
            {
                PackageDeliveryCost = 0;
                PackageDeliveryPaid = DeliveryPaymentMode.We;
            }
            else
            {
                OrderDeliveryCostDto data = new OrderDeliveryCostDto(
                    parameter.SubdivisionId,
                    parameter.CarryId,
                    parameter.BonusesQuantity,
                    parameter.PaymentId,
                    OrderProducts.Select(x => new OrderProductDeliveryCostDto(x.ProductId, x.Quantity, x.PriceOut, x.CurrencyOutId, x.FreeDelivery)));

                CostDto response = await WebClient.ExecuteApiRequestAsync(new CalculateDeliveryCost(data));

                if (parameter.IgnoreDeliveryCostCalculation)
                {
                    PackageDeliveryCost = parameter.CurrentPackageDeliveryCost;
                }
                else
                {
                    PackageDeliveryCost = response.Value;
                }

                PackageDeliveryPaid = DeliveryPaymentMode.GetFromBoolean(response.Paid);
            }
        }
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order.OrderEditPrice
{
    public class OrderEditPriceViewItem : BindableBase, IOrderPaymentInfo
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int BonusesQuantity
        {
            get { return GetProperty(() => BonusesQuantity); }
            set { SetProperty(() => BonusesQuantity, value); }
        }

        public int BonusesToChargeQuantity
        {
            get { return GetProperty(() => BonusesToChargeQuantity); }
            set { SetProperty(() => BonusesToChargeQuantity, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public int? ClientId
        {
            get { return GetProperty(() => ClientId); }
            set { SetProperty(() => ClientId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int Pko
        {
            get { return GetProperty(() => Pko); }
            set { SetProperty(() => Pko, value); }
        }

        public int PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public int? PackageDeliveryCost
        {
            get { return GetProperty(() => PackageDeliveryCost); }
            set { SetProperty(() => PackageDeliveryCost, value); }
        }

        public ObservableCollection<OrderEditPriceProductViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            set { SetProperty(() => OrderProducts, value); }
        }

        public ObservableCollection<OrderPaymentRecordViewItem> OrderPayments
        {
            get { return GetProperty(() => OrderPayments); }
            set { SetProperty(() => OrderPayments, value); }
        }

        public int? MinPriceFreeDelivery { get; } = null;

        public IEnumerable<IOrderPaymentInfoProduct> GetOrderProducts() => OrderProducts;

        public IEnumerable<IOrderPayment> GetOrderPayments() => OrderPayments;

        public int GetBonusesQuantity() => BonusesQuantity;

        public int GetBonusesToChargeQuantity() => BonusesToChargeQuantity;

        public decimal? MoneyBackAmount { get; set; }
    }
}
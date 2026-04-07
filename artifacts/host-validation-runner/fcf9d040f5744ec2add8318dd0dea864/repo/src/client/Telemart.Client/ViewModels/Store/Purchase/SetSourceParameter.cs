using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public sealed class SetSourceParameter : BindableBase
    {
        #region OrderProduct

        public int ProductRecordId
        {
            get { return GetProperty(() => ProductRecordId); }
            set { SetProperty(() => ProductRecordId, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public OrderProductStatus OrderProductState
        {
            get { return GetProperty(() => OrderProductState); }
            set { SetProperty(() => OrderProductState, value); }
        }

        public int? ProductInvoiceId
        {
            get { return GetProperty(() => ProductInvoiceId); }
            set { SetProperty(() => ProductInvoiceId, value); }
        }

        #endregion

        #region Order

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value); }
        }

        public string OrderWarehouseName
        {
            get { return GetProperty(() => OrderWarehouseName); }
            set { SetProperty(() => OrderWarehouseName, value); }
        }

        public DateTime? OrderDeliveryTime
        {
            get { return GetProperty(() => OrderDeliveryTime); }
            set { SetProperty(() => OrderDeliveryTime, value); }
        }

        public OrderStatus OrderState
        {
            get { return GetProperty(() => OrderState); }
            set { SetProperty(() => OrderState, value); }
        }

        public int? OrderPaymentId
        {
            get { return GetProperty(() => OrderPaymentId); }
            set { SetProperty(() => OrderPaymentId, value); }
        }

        #endregion
    }
}
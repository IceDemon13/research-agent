using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store
{
    [POCOViewModel]
    public class OrderViewItem : IOrder, IOrderPaymentViewItem
    {
        protected OrderViewItem()
        {
        }

        public virtual int? CityId { get; set; }

        public virtual int ClientId { get; set; }

        public int? CustomerId { get; set; }

        public int? CustomerEstId { get; set; }

        public virtual string Comment { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual DateTime? ReceiveTime { get; set; }

        public virtual DateTime? CustomerReceivedOn { get; init; }

        public virtual string Fio { get; set; }

        public virtual string LockEmployee { get; set; }

        public virtual int? EmployeeLockId { get; set; }

        public virtual string PackageTtn { get; set; }

        public virtual int PackageDeliveryCost { get; set; }

        public virtual Payment Payment { get; set; }

        public virtual string Phone { get; set; }

        public virtual string Phone2 { get; set; }

        public virtual string CustomerStateText { get; set; }

        public virtual int Pko { get; set; }

        public virtual ObservableCollection<OrderProductViewItem> Products { get; set; }

        public virtual int Rt { get; set; }

        public virtual int? LegalEntityId { get; set; }

        public virtual string StateText { get; set; }

        public virtual Prices TotalPrice { get; set; }

        public decimal? TotalCostUsd { get; set; }

        public virtual bool DontCall { get; set; }

        public virtual bool SeparateWarrantyCards { get; set; }

        public virtual bool Preorder { get; set; }

        public virtual string ProductSourceInfo { get; set; }

        public virtual string PhoneFormatted => State == OrderStatus.Received || State == OrderStatus.Confirmed || State == OrderStatus.Packed
            ? Phone
            : string.Empty;

        public virtual int? NewCallsCount { get; set; }

        public virtual CarryType Carry { get; set; }

        public virtual DateTime? DeliveryTime { get; set; }

        public virtual DateTime? DeliveryTimeTo { get; set; }

        public virtual int Id { get; set; }

        public virtual string ExternalOrderId { get; set; }

        public virtual OrderStatus State { get; set; }

        public virtual Subdivision Subdivision { get; set; }

        public virtual int? OrderSourceId { get; set; }

        public virtual int? WarehouseId { get; set; }

        public virtual int? BufferWarehouseId { get; set; }

        public virtual DateTime? MinInvoiceCloseDate { get; set; }

        public bool CompletedOnFiscalRegistrar { get; set; }

        public decimal? MoneyBackAmount { get; set; }

        public string FiscalId { get; set; }

        public bool CanceledFromSite { get; set; }

        public bool IsPaid => !(Payment != null && Payment.Id != Payment.CashId && Payment.Id != Payment.NoId && Pko != 1);

        public int ProductsCount => Products.GroupBy(x => x.ProductId).Count();

        public string NpCourierCallBarcode { get; set; }

        public string NpCourierCallInterval { get; set; }

        public virtual bool RealCompletedOnFiscalRegistrar { get; set; }

        IReadOnlyCollection<IOrderProduct> IOrder.Products => Products;

        public static OrderViewItem Create()
        {
            OrderViewItem viewItem = ViewModelSource<OrderViewItem>.Create();
            viewItem.Products = new ObservableCollection<OrderProductViewItem>();
            return viewItem;
        }

        protected void OnOptionsChanged(OrderOptionsDto oldOptions)
        {
            this.RaisePropertyChanged(x => x.DontCall);
        }

        protected void OnPaymentChanged(Payment oldValue)
        {
            this.RaisePropertyChanged(x => x.IsPaid);
        }

        protected void OnPhoneChanged(string oldPhone)
        {
            this.RaisePropertyChanged(x => x.PhoneFormatted);
        }

        protected void OnStateChanged(OrderStatus oldState)
        {
            this.RaisePropertyChanged(x => x.PhoneFormatted);
        }
    }
}
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
    public class OrderPackViewItem : IOrder, IOrderPaymentViewItem
    {
        protected OrderPackViewItem()
        {
        }

        public virtual int? CityId { get; set; }

        public virtual int ClientId { get; set; }

        public int? CustomerId { get; set; }

        public int? CustomerEstId { get; set; }

        public virtual string Comment { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual DateTime? ReceiveTime { get; set; }

        public virtual string Fio { get; set; }

        public virtual string LockEmployee { get; set; }

        public virtual int? EmployeeLockId { get; set; }

        public virtual int? EmployeePackId { get; set; }

        public virtual string PackageTtn { get; set; }

        public virtual int PackageDeliveryCost { get; set; }

        public virtual int PackagePlaces { get; set; }

        public virtual Payment Payment { get; set; }

        public virtual string Address { get; set; }

        public virtual string LastName { get; set; }

        public virtual string FirstName { get; set; }

        public virtual string MiddleName { get; set; }

        public virtual string Email { get; set; }

        public virtual string Phone { get; set; }

        public virtual string Phone2 { get; set; }

        public virtual string CustomerStateText { get; set; }

        public virtual int Pko { get; set; }

        public virtual ObservableCollection<OrderProductViewItem> Products { get; set; }

        public virtual int Rt { get; set; }

        public virtual string StateText { get; set; }

        public virtual Prices TotalPrice { get; set; }

        public decimal? TotalCostUsd { get; set; }

        public virtual OrderOptionsDto Options { get; set; }

        public bool DontCall => Options?.DontCall ?? false;

        public virtual string ProductSourceInfo { get; set; }

        public virtual string PhoneFormatted => State == OrderStatus.Received || State == OrderStatus.Confirmed || State == OrderStatus.Packed
            ? Phone
            : string.Empty;

        public virtual int? CallsCount { get; set; }

        public virtual int? NewCallsCount { get; set; }

        public virtual CarryType Carry { get; set; }

        public virtual DateTime? DeliveryTime { get; set; }

        public virtual DateTime? DeliveryTimeTo { get; set; }

        public virtual int Id { get; set; }

        public virtual OrderStatus State { get; set; }

        public virtual Subdivision Subdivision { get; set; }

        public virtual int? WarehouseId { get; set; }

        public virtual int? BufferWarehouseId { get; set; }

        public virtual int? AssemblyWarehouseId { get; set; }

        public virtual LegalEntityDto LegalEntity { get; set; }

        public virtual bool OldClient { get; set; }

        public virtual int ClientPriceTypeId { get; set; }

        public virtual DateTime? MinInvoiceCloseDate { get; set; }

        public virtual bool ReadyForPacking { get; set; }

        public bool CompletedOnFiscalRegistrar { get; set; }

        public string FiscalId { get; set; }

        public virtual bool IsCollected { get; set; }

        public int? PackListId { get; set; }

        public bool EventUnpackAndCancelOrder { get; set; }

        public bool CanceledFromSite { get; set; }

        public bool IsPaid => !(Payment != null && Payment.Id != Payment.CashId && Payment.Id != Payment.NoId && Pko != 1);

        public int ProductsCount => Products.GroupBy(x => x.ProductId).Count();

        public int? LegalEntityId => LegalEntity?.Id;

        IReadOnlyCollection<IOrderProduct> IOrder.Products => Products;

        public string NpCourierCallBarcode { get; set; }

        public string NpCourierCallInterval { get; set; }

        public static OrderPackViewItem Create()
        {
            OrderPackViewItem viewItem = ViewModelSource<OrderPackViewItem>.Create();
            viewItem.Products = new ObservableCollection<OrderProductViewItem>();
            return viewItem;
        }
    }
}
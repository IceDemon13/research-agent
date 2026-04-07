using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class PurchaseViewItem : IOrderPaymentViewItem
    {
        protected PurchaseViewItem()
        {
        }

        public virtual int? InvoiceEmployeeSupId { get; set; }

        public virtual int ProductCurrencyOutId { get; set; }

        public virtual int ProductEmployeeSupId { get; set; }

        public virtual int ProductEmployeeId { get; set; }

        public virtual int ProductId { get; set; }

        public virtual int? ProductInvoiceId { get; set; }

        public virtual int? ProductMovementId { get; set; }

        public virtual int ProductPosition { get; set; }

        public virtual bool BonusesApplied { get; set; }

        public virtual bool PricesUp => ProductPriceOut < PriceTelemart1 && ProductPriceOut != decimal.One;

        public virtual decimal ProductPrice1C { get; set; }

        public virtual decimal ProductPriceOut { get; set; }

        public virtual decimal PriceTelemart1 { get; set; }

        public virtual int ProductProductId { get; set; }

        public virtual string ProductProductName { get; set; }

        public virtual int ProductQuantity { get; set; }

        public virtual int OrderProductQuantity { get; set; }

        public virtual int ProductSourceId { get; set; }

        public virtual OrderProductSource ProductSource { get; set; }

        public virtual int ProductStateId { get; set; }

        public virtual OrderProductStatus ProductState { get; set; }

        public virtual int[] ProductCategoryIds { get; set; }

        public virtual int OrderId { get; set; }

        public virtual CarryType OrderCarryType { get; set; }

        public virtual int OrderClientId { get; set; }

        public virtual int? OrderEmployeeLockId { get; set; }

        public virtual string OrderEmployeeLockName { get; set; }

        public virtual int? OrderConfirmedBy { get; set; }

        public virtual int OrderEmployeeCreateId { get; set; }

        public virtual Payment OrderPayment { get; set; }

        public virtual OrderStatus OrderState { get; set; }

        public virtual Subdivision OrderSubdivision { get; set; }

        public virtual int? OrderWarehouseId { get; set; }

        public virtual string OrderComment { get; set; }

        public virtual DateTime? OrderReceiveTime { get; set; }

        public virtual DateTime OrderCreatedOn { get; set; }

        public virtual DateTime? OrderDeliveryTime { get; set; }

        public virtual DateTime? OrderDeliveryTimeTo { get; set; }

        public virtual int OrderPko { get; set; }

        public virtual int OrderRt { get; set; }

        public virtual int? OrderFolderTypeId { get; set; }

        public virtual int? OrderBasedOnServiceRequestId { get; set; }

        public virtual bool OrderContainsAssemblyService { get; set; }

        public virtual DateTime? OrderProductCreateOn { get; set; }

        public virtual int? ExpectationSource => ProductSource.Id == OrderProductSourceType.None.Id && OrderProductCreateOn.HasValue ? (int)DateTime.Now.Subtract(OrderProductCreateOn.Value).TotalMinutes : null;

        public virtual PurchaseEmployeeType PurchaseEmployeeType { get; set; }

        public virtual bool Категория { get; set; }

        public bool IsPaid => !(OrderPayment != null
            && OrderPayment.Id != Payment.CashId
            && OrderPayment.Id != Payment.NoId
            && OrderPko != 1);

        public static PurchaseViewItem Create()
        {
            PurchaseViewItem viewItem = ViewModelSource<PurchaseViewItem>.Create();
            viewItem.Категория = true;
            return viewItem;
        }

        protected void OnOrderPaymentChanged(Payment old)
        {
            this.RaisePropertyChanged(x => x.IsPaid);
        }
    }
}
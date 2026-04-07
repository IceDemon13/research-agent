using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order
{
    [POCOViewModel]
    public class OrderPaymentRecordViewItem : IOrderPayment
    {
        protected OrderPaymentRecordViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int OrderId { get; set; }

        public virtual Currency Currency { get; set; }

        public virtual int CashboxId { get; set; }

        public int CurrencyId => Currency.Id;

        public virtual sbyte Sign { get; set; }

        public virtual decimal Amount { get; set; }

        public virtual decimal AmountPaid { get; set; }

        public virtual int CreatedBy { get; set; }

        public virtual int? RefundId { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual RefundState State { get; set; }

        public int? PaymentId => Payment?.Id;

        public virtual Payment Payment { get; set; }

        public virtual DateTime? ReceivedOn { get; set; }

        public virtual string Comment { get; set; }

        public virtual bool CompletedOnFiscalRegistrar { get; set; }

        public virtual bool RealCompletedOnFiscalRegistrar { get; set; }

        public virtual int? FiscalCashboxId { get; set; }

        public virtual string FiscalId { get; set; }

        public virtual int? BonusTypeId { get; set; }

        public static OrderPaymentRecordViewItem Create()
        {
            return ViewModelSource<OrderPaymentRecordViewItem>.Create();
        }
    }
}
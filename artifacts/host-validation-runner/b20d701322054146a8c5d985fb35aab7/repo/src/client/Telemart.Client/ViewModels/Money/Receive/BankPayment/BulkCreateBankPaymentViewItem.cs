using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BulkCreateBankPaymentViewItem : BindableBase
    {
        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public string Reference
        {
            get { return GetProperty(() => Reference); }
            set { SetProperty(() => Reference, value); }
        }

        public DateTime? PaidOn
        {
            get { return GetProperty(() => PaidOn); }
            set { SetProperty(() => PaidOn, value); }
        }

        public decimal? Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public bool StatementSupport
        {
            get { return GetProperty(() => StatementSupport); }
            set { SetProperty(() => StatementSupport, value); }
        }

        public string ContractorName
        {
            get { return GetProperty(() => ContractorName); }
            set { SetProperty(() => ContractorName, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public decimal? Fee
        {
            get { return GetProperty(() => Fee); }
            set { SetProperty(() => Fee, value); }
        }

        public decimal? TotalAmount
        {
            get { return GetProperty(() => TotalAmount); }
            set { SetProperty(() => TotalAmount, value); }
        }

        public int? PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }
    }
}
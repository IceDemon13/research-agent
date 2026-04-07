using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BankPaymentViewItem : BindableBase, IDataErrorInfo
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? ParsedOrderId
        {
            get { return GetProperty(() => ParsedOrderId); }
            set { SetProperty(() => ParsedOrderId, value); }
        }

        public DateTime PaidOn
        {
            get { return GetProperty(() => PaidOn); }
            set { SetProperty(() => PaidOn, value); }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value, () => RaisePropertiesChanged(nameof(AllowEdit), nameof(OrderId))); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public int CashboxId
        {
            get { return GetProperty(() => CashboxId); }
            set { SetProperty(() => CashboxId, value); }
        }

        public string ContractorName
        {
            get { return GetProperty(() => ContractorName); }
            set { SetProperty(() => ContractorName, value); }
        }

        public string Reference
        {
            get { return GetProperty(() => Reference); }
            set { SetProperty(() => Reference, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public string LastError
        {
            get { return GetProperty(() => LastError); }
            set { SetProperty(() => LastError, value, () => RaisePropertyChanged(nameof(VisibleLastError))); }
        }

        public string ErrorMessage
        {
            get { return GetProperty(() => ErrorMessage); }
            set { SetProperty(() => ErrorMessage, value, () => RaisePropertyChanged(nameof(IsError))); }
        }

        public bool IsProcessed
        {
            get { return GetProperty(() => IsProcessed); }
            set { SetProperty(() => IsProcessed, value); }
        }

        public bool VisibleLastError => !string.IsNullOrEmpty(LastError);

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        public bool AllowEdit => StateId == BankPaymentState.NewId;

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<BankPaymentViewItem> builder)
        {
            builder.Property(x => x.OrderId)
                .MatchesInstanceRule((x, y) => y.StateId != BankPaymentState.NewId || x > 0, () => Resources.RequiredErrorMessage);
        }
    }
}
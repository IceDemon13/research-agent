using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class ExternalPaymentViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public Payment Payment
        {
            get { return GetProperty(() => Payment); }
            set { SetProperty(() => Payment, value, () => RaisePropertyChanged(nameof(DisplayLink))); }
        }

        public Payment ParentPayment
        {
            get { return GetProperty(() => ParentPayment); }
            set { SetProperty(() => ParentPayment, value, () => RaisePropertyChanged(nameof(ToolTip))); }
        }

        public int ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public int PaymentStateId
        {
            get { return GetProperty(() => PaymentStateId); }
            set { SetProperty(() => PaymentStateId, value); }
        }

        public string ExternalOrderId
        {
            get { return GetProperty(() => ExternalOrderId); }
            set { SetProperty(() => ExternalOrderId, value); }
        }

        public decimal CreatedAmount
        {
            get { return GetProperty(() => CreatedAmount); }
            set { SetProperty(() => CreatedAmount, value); }
        }

        public decimal? HoldedAmount
        {
            get { return GetProperty(() => HoldedAmount); }
            set { SetProperty(() => HoldedAmount, value); }
        }

        public decimal? ReceivedAmount
        {
            get { return GetProperty(() => ReceivedAmount); }
            set { SetProperty(() => ReceivedAmount, value); }
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

        public string Link
        {
            get { return GetProperty(() => Link); }
            set { SetProperty(() => Link, value, () => RaisePropertyChanged(nameof(DisplayLink))); }
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

        public ExternalPaymentParamsDto Params
        {
            get { return GetProperty(() => Params); }
            set { SetProperty(() => Params, value); }
        }

        public string DisplayLink => string.IsNullOrEmpty(Link) ? (Payment?.Id is Payment.AlfabankId or Payment.PaylaterId ? "[Потдверждение оплаты по реквизитам на почте]" : "[Подтверждение оплаты в приложении банка]") : Link;

        public string ToolTip => ParentPayment is not null ? $"Предоплата по эквайрингу '{ParentPayment.Name}'" : null;
    }
}
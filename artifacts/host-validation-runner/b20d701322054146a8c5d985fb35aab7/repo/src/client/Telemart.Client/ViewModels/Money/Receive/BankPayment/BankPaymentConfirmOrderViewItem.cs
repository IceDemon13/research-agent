using System.Collections.Generic;
using DevExpress.Mvvm;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BankPaymentConfirmOrderViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public OrderStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public Payment Payment
        {
            get { return GetProperty(() => Payment); }
            set { SetProperty(() => Payment, value, () => RaisePropertyChanged(nameof(IsPaid))); }
        }

        public OrderPrices Prices
        {
            get { return GetProperty(() => Prices); }
            set { SetProperty(() => Prices, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public IReadOnlyCollection<ExternalPaymentDto> ExternalPayments
        {
            get { return GetProperty(() => ExternalPayments); }
            set { SetProperty(() => ExternalPayments, value); }
        }

        public int Pko
        {
            get { return GetProperty(() => Pko); }
            set { SetProperty(() => Pko, value, () => RaisePropertyChanged(nameof(IsPaid))); }
        }

        public bool IsPaid => Payment == null ||
                            Payment.Id == Payment.CashId ||
                            Payment.Id == Payment.NoId ||
                            Pko == 1;
    }
}
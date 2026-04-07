using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductPurchaseHistoryViewItem : BindableBase
    {
        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public string ContractorName
        {
            get { return GetProperty(() => ContractorName); }
            set { SetProperty(() => ContractorName, value); }
        }

        public DateTime DateClose
        {
            get { return GetProperty(() => DateClose); }
            set { SetProperty(() => DateClose, value); }
        }

        public decimal PriceUah
        {
            get { return GetProperty(() => PriceUah); }
            set { SetProperty(() => PriceUah, value); }
        }

        public bool LastPurchase
        {
            get { return GetProperty(() => LastPurchase); }
            set { SetProperty(() => LastPurchase, value); }
        }

        public decimal PriceUsd
        {
            get { return GetProperty(() => PriceUsd); }
            set { SetProperty(() => PriceUsd, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public string DisplayPrice
        {
            get { return GetProperty(() => DisplayPrice); }
            set { SetProperty(() => DisplayPrice, value); }
        }
    }
}

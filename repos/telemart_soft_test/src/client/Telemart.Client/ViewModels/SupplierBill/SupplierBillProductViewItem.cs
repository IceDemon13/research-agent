using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public class SupplierBillProductViewItem : BindableBase, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int BillId
        {
            get { return GetProperty(() => BillId); }
            set { SetProperty(() => BillId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public TaxRate TaxRate
        {
            get { return GetProperty(() => TaxRate); }
            set { SetProperty(() => TaxRate, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public decimal PriceTax
        {
            get { return GetProperty(() => PriceTax); }
            set { SetProperty(() => PriceTax, value); }
        }

        public decimal Sum
        {
            get { return GetProperty(() => Sum); }
            set { SetProperty(() => Sum, value); }
        }

        public decimal SumTax
        {
            get { return GetProperty(() => SumTax); }
            set { SetProperty(() => SumTax, value); }
        }

        public long? Tnved
        {
            get { return GetProperty(() => Tnved); }
            set { SetProperty(() => Tnved, value); }
        }

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);
    }
}
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceDelayProductViewItem : TelemartViewItemBase, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string FullName
        {
            get { return GetProperty(() => FullName); }
            set { SetProperty(() => FullName, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string FullNameUa
        {
            get { return GetProperty(() => FullNameUa); }
            set { SetProperty(() => FullNameUa, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string FullNameEn
        {
            get { return GetProperty(() => FullNameEn); }
            set { SetProperty(() => FullNameEn, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public int OrderQuantity
        {
            get { return GetProperty(() => OrderQuantity); }
            set { SetProperty(() => OrderQuantity, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int AlternativeId
        {
            get { return GetProperty(() => AlternativeId); }
            set { SetProperty(() => AlternativeId, value); }
        }

        public short RemoveSource
        {
            get { return GetProperty(() => RemoveSource); }
            set { SetProperty(() => RemoveSource, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public string DisplayName => this.GetLocalName(LocalizableNameType.Ukr);

        public string Name => FullName;

        public string NameUkr => FullNameUa;

        public string NameEn => FullNameEn;
    }
}
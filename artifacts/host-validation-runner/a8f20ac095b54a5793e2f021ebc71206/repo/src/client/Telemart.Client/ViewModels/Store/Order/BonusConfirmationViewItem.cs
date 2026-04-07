using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Common.Bonuses;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class BonusConfirmationViewItem : BindableBase, IBonusProduct
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public decimal PriceCurrent
        {
            get { return GetProperty(() => PriceCurrent); }
            set { SetProperty(() => PriceCurrent, value, () => RaisePropertyChanged(nameof(PriceNew))); }
        }

        public decimal PriceNew => PriceCurrent - Quantity;

        public int CurrencyId { get; } = Currency.UahId;

        public int MaxQuantity
        {
            get { return GetProperty(() => MaxQuantity); }
            set { SetProperty(() => MaxQuantity, value, () => RaisePropertyChanged(nameof(Quantity))); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertyChanged(nameof(PriceNew))); }
        }

        public static void BuildMetadata(MetadataBuilder<BonusConfirmationViewItem> builder)
        {
            builder.Property(x => x.Quantity)
                .MatchesInstanceRule((x, y) => x <= y.MaxQuantity, () => "Превышен лимит бонусов по товару");
        }
    }
}
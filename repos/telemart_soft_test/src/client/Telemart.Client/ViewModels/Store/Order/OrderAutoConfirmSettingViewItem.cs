using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderAutoConfirmSettingViewItem : TelemartCloneableViewItemBase
    {
        public OrderAutoConfirmSettingViewItem()
        {
            OrderProductSources = new List<object>();
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public CarryType Carry
        {
            get { return GetProperty(() => Carry); }
            set { SetProperty(() => Carry, value); }
        }

        public Payment Payment
        {
            get { return GetProperty(() => Payment); }
            set { SetProperty(() => Payment, value); }
        }

        public decimal? MaxSumLimit
        {
            get { return GetProperty(() => MaxSumLimit); }
            set { SetProperty(() => MaxSumLimit, value); }
        }

        public double? MinExtraChargePercent
        {
            get { return GetProperty(() => MinExtraChargePercent); }
            set { SetProperty(() => MinExtraChargePercent, value); }
        }

        public int? MaxProductQuantity
        {
            get { return GetProperty(() => MaxProductQuantity); }
            set { SetProperty(() => MaxProductQuantity, value); }
        }

        public int? MaxLinesQuantity
        {
            get { return GetProperty(() => MaxLinesQuantity); }
            set { SetProperty(() => MaxLinesQuantity, value); }
        }

        public List<object> OrderProductSources
        {
            get { return GetProperty(() => OrderProductSources); }
            set { SetProperty(() => OrderProductSources, value); }
        }

        public static void BuildMetadata(MetadataBuilder<OrderAutoConfirmSettingViewItem> builder)
        {
            builder.Property(x => x.Carry)
                .MatchesRule(x => x is null || x.Active, () => "Способ доставки не активен")
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Payment)
                .MatchesRule(x => x is null || x.Active, () => "Способ оплаты не активен")
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MaxProductQuantity)
                .MatchesRule(x => x is > 0 and < 100, () => "Значение должно быть в диапазоне от 1 до 99");
            builder.Property(x => x.MaxLinesQuantity)
                .MatchesRule(x => x is > 0 and < 100, () => "Значение должно быть в диапазоне от 1 до 99");
            builder.Property(x => x.MinExtraChargePercent)
                .MatchesRule(x => x is >= -50 and < 100, () => "Значение должно быть в диапазоне от -50 до 99");
            builder.Property(x => x.MaxSumLimit)
                .MatchesRule(x => x is > 50 and < 1_000_000, () => "Значение должно быть в диапазоне от 50 до 999999");
            builder.Property(x => x.OrderProductSources)
                .MatchesRule(x => x?.Any() == true, () => "Должен быть заполнен как минимум 1 источник");
        }
    }
}
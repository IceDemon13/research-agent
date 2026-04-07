using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public sealed class SupplierCurrencyCreateItem : TelemartViewItemBase
    {
        public SupplierCurrencyCreateItem(int id, int supplierId, int currencyId, decimal rate)
        {
            Id = id;
            SupplierId = supplierId;
            CurrencyId = currencyId;
            RateNew = RateOld = rate;
        }

        public SupplierCurrencyCreateItem()
        {
        }

        public SupplierCurrencyCreateItem(int supplierId, int currencyId)
        {
            SupplierId = supplierId;
            CurrencyId = currencyId;
        }

        public int? Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value, ChangeSupplierOrCurrency); }
        }

        public string SupplerName
        {
            get { return GetProperty(() => SupplerName); }
            set { SetProperty(() => SupplerName, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value, ChangeSupplierOrCurrency); }
        }

        public decimal RateNew
        {
            get { return GetProperty(() => RateNew); }
            set { SetProperty(() => RateNew, value, ChancgeDeviation); }
        }

        public decimal RateOld
        {
            get { return GetProperty(() => RateOld); }
            set { SetProperty(() => RateOld, value); }
        }

        public decimal Deviation
        {
            get { return GetProperty(() => Deviation); }
            set { SetProperty(() => Deviation, value, () => RaisePropertiesChanged(nameof(DeviationUp), nameof(DeviationDown))); }
        }

        public bool IsCopySupplierCurrencyRate
        {
            get { return GetProperty(() => IsCopySupplierCurrencyRate); }
            set { SetProperty(() => IsCopySupplierCurrencyRate, value, () => RaisePropertiesChanged(nameof(CurrencyId), nameof(SupplierId))); }
        }

        public bool IsChanged => RateNew != RateOld || Id is null;

        public bool DeviationUp => RateNew > 0 && RateOld > 0 && RateNew > RateOld;

        public bool DeviationDown => RateNew > 0 && RateOld > 0 && RateOld > RateNew;

        public static void BuildMetadata(MetadataBuilder<SupplierCurrencyCreateItem> builder)
        {
            builder.Property(x => x.RateNew)
                .MatchesInstanceRule((x, y) => x is >= 0.01m and <= 999.99m, () => "Доступные значения 0.01....999.99");
            builder.Property(x => x.CurrencyId)
                .MatchesRule(x => x > 0, () => "Не заполнена валюта")
                .MatchesInstanceRule((x, y) => !y.IsCopySupplierCurrencyRate, () => "Уже есть запись с таким поставщиком и валютой");
            builder.Property(x => x.SupplierId)
                .MatchesRule(x => x > 0, () => "Не заполнен контрагент")
                .MatchesInstanceRule((x, y) => !y.IsCopySupplierCurrencyRate, () => "Уже есть запись с таким поставщиком и валютой");
        }

        private void ChancgeDeviation()
        {
            if (RateNew > 0 && RateOld > 0)
            {
                Deviation = RateOld == RateNew
                    ? 0
                    : Math.Round(100 * (RateNew - RateOld) / RateOld, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                Deviation = 0;
            }

            RaisePropertyChanged(nameof(IsChanged));
        }

        private void ChangeSupplierOrCurrency()
        {
            IsCopySupplierCurrencyRate = false;
        }
    }
}
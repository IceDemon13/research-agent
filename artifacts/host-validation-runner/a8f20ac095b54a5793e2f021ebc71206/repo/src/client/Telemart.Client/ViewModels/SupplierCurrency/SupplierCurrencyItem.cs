using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public sealed class SupplierCurrencyItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public decimal Rate
        {
            get { return GetProperty(() => Rate); }
            set { SetProperty(() => Rate, value); }
        }

        public decimal Deviation
        {
            get { return GetProperty(() => Deviation); }
            set { SetProperty(() => Deviation, value, () => RaisePropertiesChanged(nameof(DeviationUp), nameof(DeviationDown))); }
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

        public bool DeviationUp => Deviation > 0m;

        public bool DeviationDown => Deviation < 0m;
    }
}
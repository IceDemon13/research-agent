using System;
using Telemart.Client.Business;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.LogisticsAnalitics
{
    public sealed class OrderLogisticsAnaliticsViewItem : TelemartCloneableViewItemBase
    {
        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public OrderStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public DateTime? DeliveryTime
        {
            get { return GetProperty(() => DeliveryTime); }
            set { SetProperty(() => DeliveryTime, value); }
        }

        public DateTime? DeliveryTimeTo
        {
            get { return GetProperty(() => DeliveryTimeTo); }
            set { SetProperty(() => DeliveryTimeTo, value); }
        }

        public long Overdue => GetOverdue();

        public int Rows
        {
            get { return GetProperty(() => Rows); }
            set { SetProperty(() => Rows, value); }
        }

        public double Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value, () => RaisePropertyChanged(nameof(OrdersPriceString))); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public bool ReadyForPacking
        {
            get { return GetProperty(() => ReadyForPacking); }
            set { SetProperty(() => ReadyForPacking, value); }
        }

        public bool EventUnpackAndCancelOrder
        {
            get { return GetProperty(() => EventUnpackAndCancelOrder); }
            set { SetProperty(() => EventUnpackAndCancelOrder, value); }
        }

        public bool CanceledFromSite
        {
            get { return GetProperty(() => CanceledFromSite); }
            set { SetProperty(() => CanceledFromSite, value); }
        }

        public string OrdersPriceString => CurrencyFormatingRules.ToUahStr(Price);

        public long GetOverdue()
        {
            if (DeliveryTimeTo.HasValue)
            {
                return DeliveryTimeTo.Value.GetMinutesDateTimeRelativeNow();
            }

            return 0;
        }
    }
}
using System;
using Telemart.Client.Business;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.LogisticsAnalitics
{
    public sealed class InvoiceLogisticsAnaliticsViewItem : TelemartCloneableViewItemBase
    {
        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public InvoiceState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public DateTime DateArrive
        {
            get { return GetProperty(() => DateArrive); }
            set { SetProperty(() => DateArrive, value); }
        }

        public long OverdueDateArrive => GetOverdueDateArrive();

        public DateTime DateReceive
        {
            get { return GetProperty(() => DateReceive); }
            set { SetProperty(() => DateReceive, value); }
        }

        public long OverdueDateReceive => GetOverdueDateReceive();

        public int Rows
        {
            get { return GetProperty(() => Rows); }
            set { SetProperty(() => Rows, value); }
        }

        public int OrderRows
        {
            get { return GetProperty(() => OrderRows); }
            set { SetProperty(() => OrderRows, value); }
        }

        public int OrdersCount
        {
            get { return GetProperty(() => OrdersCount); }
            set { SetProperty(() => OrdersCount, value); }
        }

        public double Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public decimal OrdersPrice
        {
            get { return GetProperty(() => OrdersPrice); }
            set { SetProperty(() => OrdersPrice, value, () => RaisePropertyChanged(nameof(OrdersPriceString))); }
        }

        public string OrdersPriceString => CurrencyFormatingRules.ToStr(OrdersPrice,  Currency.Usd.Id);

        private long GetOverdueDateArrive()
        {
            if (State.Id < InvoiceState.Arrived.Id)
            {
                return DateArrive.GetMinutesDateTimeRelativeNow();
            }

            return 0;
        }

        private long GetOverdueDateReceive()
        {
            if (State.Id < InvoiceState.Received.Id)
            {
                return DateReceive.GetMinutesDateTimeRelativeNow();
            }

            return 0;
        }
    }
}
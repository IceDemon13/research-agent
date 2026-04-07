using System;
using System.Linq;
using Telemart.Client.Business;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.LogisticsAnalitics
{
    public sealed class MovementLogisticsAnaliticsViewItem : TelemartCloneableViewItemBase
    {
        public int MovementId
        {
            get { return GetProperty(() => MovementId); }
            set { SetProperty(() => MovementId, value); }
        }

        public int WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public MovementState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public DateTime DateRecievSent
        {
            get { return GetProperty(() => DateRecievSent); }
            set { SetProperty(() => DateRecievSent, value); }
        }

        public DateTime DateSend
        {
            get { return GetProperty(() => DateSend); }
            set { SetProperty(() => DateSend, value); }
        }

        public DateTime DateArrive
        {
            get { return GetProperty(() => DateSend); }
            set { SetProperty(() => DateSend, value); }
        }

        public DateTime DateReceive
        {
            get { return GetProperty(() => DateSend); }
            set { SetProperty(() => DateSend, value); }
        }

        public long Overdue => GetOverdue();

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

        public string Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public DateTime GetDateByState()
        {
            return State.Id switch
            {
                MovementState.NewId => DateSend,
                MovementState.LeftId => DateReceive,
                MovementState.ReceivedId => DateArrive,
                _ => DateArrive
            };
        }

        public long GetOverdue()
         {
             if (State.Id < MovementState.Cancelled.Id)
             {
                 return GetDateByState().GetMinutesDateTimeRelativeNow();
             }

             return 0;
        }

        public string GetTextByStatus()
        {
            return State.Id switch
            {
                MovementState.NewId => "отправки",
                MovementState.LeftId => "принятия",
                MovementState.ReceivedId => "приезда",
                _ => string.Empty
            };
        }

        public void SetTypeMovement(int[] warehouseId)
        {
            bool sent = warehouseId.Contains(WarehouseFromId);
            bool recieve = warehouseId.Contains(WarehouseToId);

            Type = recieve ? "Входящее" :
                sent ? "Исходящее" : string.Empty;

            DateRecievSent = recieve ? DateReceive : DateSend;

            RaisePropertiesChanged(nameof(Type), nameof(DateRecievSent));
        }
    }
}
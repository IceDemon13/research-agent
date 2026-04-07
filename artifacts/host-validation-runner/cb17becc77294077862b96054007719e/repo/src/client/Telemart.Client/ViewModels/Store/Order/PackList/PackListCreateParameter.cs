using System;
using System.Collections.Generic;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public class PackListCreateParameter
    {
        public PackListCreateParameter(
            OrderStatus state,
            ComboBoxItem warehouse,
            HashSet<int> warehouseDeliveryCarryIds,
            HashSet<int> availableCarryIds,
            DateTime selectedTime,
            DateTime selectedDate)
        {
            State = state;
            Warehouse = warehouse;
            WarehouseDeliveryCarryIds = warehouseDeliveryCarryIds;
            AvailableCarryIds = availableCarryIds;
            Time = selectedTime;
            Date = selectedDate;
        }

        public OrderStatus State { get; }

        public ComboBoxItem Warehouse { get; }

        public HashSet<int> WarehouseDeliveryCarryIds { get; }

        public HashSet<int> AvailableCarryIds { get; }

        public DateTime Time { get; }

        public DateTime Date { get; }
    }
}
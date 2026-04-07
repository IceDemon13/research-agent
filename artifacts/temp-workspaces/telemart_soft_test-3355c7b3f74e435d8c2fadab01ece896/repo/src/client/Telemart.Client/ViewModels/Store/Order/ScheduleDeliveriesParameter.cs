using System;
using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ScheduleDeliveriesParameter
    {
        public ScheduleDeliveriesParameter(int carryId, int cityId, DateOnly date, IReadOnlyCollection<OrderDto> orders)
        {
            CarryId = carryId;
            CityId = cityId;
            Date = date;
            Orders = orders;
        }

        public int CarryId { get; }

        public int CityId { get; }

        public DateOnly Date { get; }

        public IReadOnlyCollection<OrderDto> Orders { get; }
    }
}
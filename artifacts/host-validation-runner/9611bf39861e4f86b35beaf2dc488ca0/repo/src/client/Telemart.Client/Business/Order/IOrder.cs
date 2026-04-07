using System;
using System.Collections.Generic;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.Order
{
    public interface IOrder
    {
        int Id { get; }

        DateTime? DeliveryTime { get; }

        DateTime? DeliveryTimeTo { get; }

        OrderStatus State { get; }

        IReadOnlyCollection<IOrderProduct> Products { get; }
    }
}

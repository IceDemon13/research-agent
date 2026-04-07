using System;
using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Order
{
    public record OrderDeliveryTime(
        DateTime? DeliveryTimeFrom,
        DateTime? DeliveryTimeTo,
        IReadOnlyCollection<DateTime> AssemblyDates,
        IReadOnlyCollection<DateTime> AdditionalServiceDates,
        IReadOnlyDictionary<int, DateTime> OrderProductsDates,
        TimeSpan? TotalAdditionalServiceEstimate,
        IReadOnlyCollection<OrderLogisticsAddіtionalServiceQuotaDto> AdditionalServiceQuotas);
}
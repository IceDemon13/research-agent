using System;
using System.Collections.Generic;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Reports.Order
{
    public sealed class OrderPackListReportData
    {
        public OrderPackListReportData(
            int number,
            IReadOnlyCollection<OrderPackListProductReportData> products,
            int ordersCount,
            Subdivision subdivision,
            double totalWeight,
            string createdByName,
            DateTime createdOn)
        {
            Products = products;
            Number = number;
            OrderCount = ordersCount;
            SubdivisionName = subdivision.Name;
            TotalWeight = totalWeight;
            CreatedByName = createdByName;
            CreatedOn = createdOn;
        }

        public IReadOnlyCollection<OrderPackListProductReportData> Products { get; }

        public int Number { get; }

        public int OrderCount { get; }

        public string SubdivisionName { get; }

        public double TotalWeight { get; }

        public string CreatedByName { get; }

        public DateTime CreatedOn { get; }

        public string DateTimeAssemblyPrint { get; private set; }

        public void SetDateTimeAssemblyPrint(DateTime dateTimeAssemblyPrint)
        {
            DateTimeAssemblyPrint = $"Дата печати: {dateTimeAssemblyPrint:dd.MM.yyyy HH:mm}";
        }
    }
}
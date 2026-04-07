using System.Collections.Generic;
using Telemart.Client.Reports.Product;

namespace Telemart.Client.Reports.Order.Assembly
{
    public class AssemblyReportData
    {
        public AssemblyReportData(string createdByName, int orderId, int? assemblyServiceId, int assemblyCount, int assemblyNumber, IReadOnlyCollection<BarcodeReportData> products)
        {
            CreatedByName = createdByName;
            OrderId = orderId;
            AssemblyServiceIdText = assemblyServiceId.HasValue ? assemblyServiceId.Value.ToString() : "{сборка не создана}";
            AssemblyCount = assemblyCount;
            AssemblyNumber = assemblyNumber;
            Products = products;
        }

        public string CreatedByName { get; }

        public int OrderId { get; }

        public string AssemblyServiceIdText { get; }

        public int AssemblyCount { get; }

        public int AssemblyNumber { get; }

        public IReadOnlyCollection<BarcodeReportData> Products { get; }
    }
}

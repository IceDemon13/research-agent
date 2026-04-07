using System.Collections.Generic;

namespace Telemart.Client.Reports.AssemblyService
{
    public class AssemblyServiceReportData
    {
        public AssemblyServiceReportData(string createdByName, int orderId, int assemblyId, IReadOnlyCollection<AssemblyServiceBarcodeReportData> products)
        {
            CreatedByName = createdByName;
            OrderId = orderId;
            Products = products;
            AssemblyId = assemblyId;
        }

        public string CreatedByName { get; }

        public int OrderId { get; }

        public int AssemblyId { get; }

        public IReadOnlyCollection<AssemblyServiceBarcodeReportData> Products { get; }
    }
}

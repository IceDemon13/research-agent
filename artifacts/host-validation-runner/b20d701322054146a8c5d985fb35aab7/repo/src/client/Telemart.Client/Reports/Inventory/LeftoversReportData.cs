using System;
using System.Collections.Generic;

namespace Telemart.Client.Reports.Inventory
{
    public class LeftoversReportData
    {
        public LeftoversReportData(int inventoryId, string employeeName, string warehouseName, IReadOnlyCollection<LeftoversCategoryReportData> categories)
        {
            InventoryId = inventoryId;
            EmployeeName = employeeName;
            WarehouseName = warehouseName;
            Categories = categories;
            PrintDate = DateTime.Now;
        }

        public int InventoryId { get; set; }

        public string EmployeeName { get; set; }

        public string WarehouseName { get; set; }

        public DateTime PrintDate { get; set; }

        public IReadOnlyCollection<LeftoversCategoryReportData> Categories { get; set; }
    }
}

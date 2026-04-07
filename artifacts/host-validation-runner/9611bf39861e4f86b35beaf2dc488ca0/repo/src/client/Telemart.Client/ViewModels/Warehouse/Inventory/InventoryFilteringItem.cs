using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    public class InventoryFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public DateTime? DateAfter { get; set; }

        public DateTime? DateBefore { get; set; }

        public string InventoryNumbers { get; set; }

        public List<int> Warehouses { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrEmpty(InventoryNumbers))
            {
                yield return ("ids", InventoryNumbers);
            }

            if (DateAfter.HasValue)
            {
                yield return ("date_after", DateAfter.Value.ToString("s"));
            }

            if (DateBefore.HasValue)
            {
                yield return ("date_before", DateBefore.Value.ToString("s"));
            }

            if (Warehouses != null && Warehouses.Any())
            {
                string warehouses = string.Join(Separator, Warehouses);
                yield return ("warehouses", warehouses);
            }
        }
    }
}
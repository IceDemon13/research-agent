using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public class SupplierCurrencyFilterItem : FilteringItemBase
    {
        [FilteringItemProperty("date_from")]
        public DateTime? DateFrom { get; set; }

        [FilteringItemProperty("date_to")]
        public DateTime? DateTo { get; set; }

        [FilteringItemProperty("supplier_ids")]
        public List<int> SupplierIds { get; set; }
    }
}
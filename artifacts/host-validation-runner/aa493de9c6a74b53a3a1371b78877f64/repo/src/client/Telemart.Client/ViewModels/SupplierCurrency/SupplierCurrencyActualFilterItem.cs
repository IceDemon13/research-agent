using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public sealed class SupplierCurrencyActualFilterItem : FilteringItemBase
    {
        public SupplierCurrencyActualFilterItem(params int[] supplierIds)
        {
            SupplierIds = supplierIds;
        }

        [FilteringItemProperty("supplier_ids")]
        public int[] SupplierIds { get; }
    }
}
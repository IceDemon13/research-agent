using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class QueryContractors : QueryEntitiesPagedRequestBase<ContractorDto>
    {
        public QueryContractors(bool? active = null, bool withExcelSettings = false, bool? anySupplierWarehouses = null, DateTime? modifiedOnAfter = null)
            : base(new ContractorFilteringItem(active, withExcelSettings, anySupplierWarehouses, modifiedOnAfter), ApiResources.Contractors)
        {
        }

        public sealed class ContractorFilteringItem : ModifiedOnAfterFilteringItem
        {
            public ContractorFilteringItem(bool? active, bool withExcelSettings, bool? anySupplierWarehouses, DateTime? modifiedOnAfter)
            {
                Active = active;
                WithExcelSettings = withExcelSettings;
                AnySupplierWarehouses = anySupplierWarehouses;
                ModifiedOnAfter = modifiedOnAfter;
            }

            [FilteringItemProperty("active")]
            public bool? Active { get; }

            [FilteringItemProperty("any_supplier_warehouses")]
            public bool? AnySupplierWarehouses { get; }

            [FilteringItemProperty("with_excel_settings")]
            public bool WithExcelSettings { get; }
        }
    }
}
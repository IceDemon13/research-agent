using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.SupplierCategoryAbc
{
    public sealed class QuerySupplierCategoriesAbc : QueryEntityRequestBase<List<SupplierCategoryAbcDto>>
    {
        public QuerySupplierCategoriesAbc(int contractorId)
            : base(ApiResources.Contractors, contractorId, "abc")
        {
        }
    }
}
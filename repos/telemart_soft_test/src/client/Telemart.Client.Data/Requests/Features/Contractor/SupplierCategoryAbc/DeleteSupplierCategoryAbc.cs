using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Contractor.SupplierCategoryAbc
{
    public sealed class DeleteSupplierCategoryAbc : DeleteEntityRequestBase
    {
        public DeleteSupplierCategoryAbc(int contractorId, int supplierCategoryAbcId)
            : base(ApiResources.Contractors, contractorId.ToString(), "abc", supplierCategoryAbcId.ToString())
        {
        }
    }
}
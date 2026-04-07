using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.SupplierCategoryAbc
{
    public sealed class CreateSupplierCategoryAbc : CreateEntityResultRequestBase<SupplierCategoryAbcDto, SupplierCategoryAbcSaveDto>
    {
        public CreateSupplierCategoryAbc(int contractorId, SupplierCategoryAbcSaveDto dto)
            : base(dto, ApiResources.Contractors, contractorId.ToString(), "abc")
        {
        }
    }
}
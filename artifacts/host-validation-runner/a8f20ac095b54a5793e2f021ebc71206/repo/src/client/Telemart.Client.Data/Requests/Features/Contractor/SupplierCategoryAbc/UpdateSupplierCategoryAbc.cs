using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.SupplierCategoryAbc
{
    public sealed class UpdateSupplierCategoryAbc : UpdateEntityResultRequestBase<SupplierCategoryAbcDto, SupplierCategoryAbcSaveDto>
    {
        public UpdateSupplierCategoryAbc(SupplierCategoryAbcSaveDto dto)
            : base(dto, ApiResources.Contractors, dto.ContractorId, "abc", dto.Id)
        {
        }
    }
}
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.CompanyStructure;

namespace Telemart.Client.Data.Requests.Features.CompanyStructure
{
    public sealed class UpdateDepartment : UpdateEntityResultRequestBase<DepartmentDto, DepartmentUpdateDto>
    {
        public UpdateDepartment(DepartmentUpdateDto updateDto)
        : base(updateDto, ApiResources.CompanyStructure, updateDto.Id, "department")
        {
        }
    }
}
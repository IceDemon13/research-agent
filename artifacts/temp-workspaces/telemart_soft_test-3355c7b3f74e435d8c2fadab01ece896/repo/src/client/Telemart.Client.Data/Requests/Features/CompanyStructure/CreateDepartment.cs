using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.CompanyStructure;

namespace Telemart.Client.Data.Requests.Features.CompanyStructure
{
    public sealed class CreateDepartment : CreateEntityResultRequestBase<DepartmentDto, DepartmentCreateDto>
    {
        public CreateDepartment(DepartmentCreateDto createDto)
            : base(createDto, ApiResources.CompanyStructure, "department")
        {
        }
    }
}
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class CreateEmployee : CreateEntityResultRequestBase<EmployeeRichDto, EmployeeCreateDto>
    {
        public CreateEmployee(EmployeeCreateDto dto)
            : base(dto, ApiResources.Employees)
        {
        }
    }
}

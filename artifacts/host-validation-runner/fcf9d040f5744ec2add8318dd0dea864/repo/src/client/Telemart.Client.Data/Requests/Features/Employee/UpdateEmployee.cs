using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class UpdateEmployee : UpdateEntityRequestBase<Result<EmployeeRichDto>, EmployeeUpdateDto>
    {
        public UpdateEmployee(EmployeeUpdateDto dto)
            : base(dto, ApiResources.Employees, dto.Id)
        {
        }
    }
}
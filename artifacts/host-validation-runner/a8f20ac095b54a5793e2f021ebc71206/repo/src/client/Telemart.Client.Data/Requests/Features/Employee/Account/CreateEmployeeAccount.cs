using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.WorkAccount;

namespace Telemart.Client.Data.Requests.Features.Employee.Account
{
    public class CreateEmployeeAccount : CreateEntityResultRequestBase<EmployeeAccountDto, EmployeeAccountCreateDto>
    {
        public CreateEmployeeAccount(int employeeId, EmployeeAccountCreateDto dto)
            : base(dto, ApiResources.Employees, employeeId.ToString(), "accounts")
        {
        }
    }
}
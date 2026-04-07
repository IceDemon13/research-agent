using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class DeleteEmployee : DeleteEntityResultRequestBase<EmployeeRichDto>
    {
        public DeleteEmployee(int id)
            : base(ApiResources.Employees, id)
        {
        }
    }
}
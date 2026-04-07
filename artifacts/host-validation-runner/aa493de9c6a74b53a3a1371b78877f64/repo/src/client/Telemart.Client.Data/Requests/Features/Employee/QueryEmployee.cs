using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class QueryEmployee : QueryEntityRequestBase<EmployeeRichDto>
    {
        public QueryEmployee(int employeeId)
            : base(ApiResources.Employees, employeeId)
        {
        }
    }
}
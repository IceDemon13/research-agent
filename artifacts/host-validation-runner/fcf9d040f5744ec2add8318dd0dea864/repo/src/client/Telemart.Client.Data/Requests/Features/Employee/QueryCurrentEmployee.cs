using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class QueryCurrentEmployee : QueryEntityRequestBase<EmployeeContextDto>
    {
        public QueryCurrentEmployee()
            : base(ApiResources.Employees, "current")
        {
        }
    }
}
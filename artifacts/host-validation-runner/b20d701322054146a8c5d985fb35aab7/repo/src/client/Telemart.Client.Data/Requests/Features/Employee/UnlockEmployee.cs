using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class UnlockEmployee : UnlockRequestBase<EmployeeRichDto>
    {
        public UnlockEmployee(int id, bool force = false)
            : base(force, ApiResources.Employees, id)
        {
        }
    }
}
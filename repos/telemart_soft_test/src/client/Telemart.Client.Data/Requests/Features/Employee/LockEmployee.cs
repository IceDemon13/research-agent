using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class LockEmployee : LockRequestBase<EmployeeRichDto>
    {
        public LockEmployee(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Employees, id)
        {
        }
    }
}
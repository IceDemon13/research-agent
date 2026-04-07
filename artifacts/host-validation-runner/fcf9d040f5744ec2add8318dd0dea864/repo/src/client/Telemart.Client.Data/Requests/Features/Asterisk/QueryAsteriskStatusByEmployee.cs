using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Asterisk;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class QueryAsteriskStatusByEmployee : QueryEntityRequestBase<AsteriskEmployeeStatusDto>
    {
        public QueryAsteriskStatusByEmployee(int employeeId)
            : base(ApiResources.Asterisk, "employee_statuses", employeeId)
        {
        }
    }
}
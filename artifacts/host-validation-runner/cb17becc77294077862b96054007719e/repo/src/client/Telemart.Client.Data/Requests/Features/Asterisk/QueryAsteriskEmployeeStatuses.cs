using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Asterisk;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class QueryAsteriskEmployeeStatuses : QueryEntitiesRequestBase<AsteriskEmployeeStatusDto>
    {
        public QueryAsteriskEmployeeStatuses()
            : base(ApiResources.Asterisk, "employee_statuses")
        {
        }
    }
}
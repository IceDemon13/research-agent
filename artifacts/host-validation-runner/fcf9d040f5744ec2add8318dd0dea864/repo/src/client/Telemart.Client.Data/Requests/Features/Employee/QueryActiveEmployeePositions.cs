using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public class QueryActiveEmployeePositions : QueryEntitiesRequestBase<EmployeePositionDto>
    {
        public QueryActiveEmployeePositions()
            : base($"{ApiResources.Employees}/active/positions")
        {
        }
    }
}

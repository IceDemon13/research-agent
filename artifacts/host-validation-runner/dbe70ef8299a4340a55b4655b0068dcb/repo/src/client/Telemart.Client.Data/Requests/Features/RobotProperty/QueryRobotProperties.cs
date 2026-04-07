using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.RobotProperty;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class QueryRobotProperties : QueryEntitiesRequestBase<RobotPropertyDto>
    {
        public QueryRobotProperties()
            : base(null, ApiResources.RobotProperties)
        {
        }
    }
}
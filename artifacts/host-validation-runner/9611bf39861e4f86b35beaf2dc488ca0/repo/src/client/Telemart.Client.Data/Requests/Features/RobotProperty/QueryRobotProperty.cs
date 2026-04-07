using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.RobotProperty;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class QueryRobotProperty : QueryEntityRequestBase<RobotPropertyDto>
    {
        public QueryRobotProperty(object id)
            : base(ApiResources.RobotProperties, id)
        {
        }
    }
}
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public class QueryRobotScript : QueryEntityRequestBase<RobotScriptDto>
    {
        public QueryRobotScript(int categoryId)
            : base(ApiResources.Categories, categoryId, "robot_script")
        {
        }
    }
}
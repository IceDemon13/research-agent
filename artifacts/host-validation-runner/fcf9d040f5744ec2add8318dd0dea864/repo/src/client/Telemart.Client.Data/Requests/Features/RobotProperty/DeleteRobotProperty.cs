using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class DeleteRobotProperty : DeleteEntityResultRequestBase<object>
    {
        public DeleteRobotProperty(int id)
            : base($"{ApiResources.RobotProperties}/{id}")
        {
        }
    }
}
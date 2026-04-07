using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class UpdateRobotProperty : UpdateEntityRequestBase<Result<RobotPropertyDto>, RobotPropertyDto>
    {
        public UpdateRobotProperty(int id, RobotPropertyDto dto)
            : base(dto, ApiResources.RobotProperties, id)
        {
        }
    }
}
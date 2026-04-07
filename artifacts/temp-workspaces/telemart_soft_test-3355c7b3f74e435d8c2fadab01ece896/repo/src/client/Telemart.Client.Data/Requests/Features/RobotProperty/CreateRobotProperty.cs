using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class CreateRobotProperty : CreateEntityRequestBase<Result<RobotPropertyDto>, RobotPropertyDto>
    {
        public CreateRobotProperty(RobotPropertyDto dto)
            : base(dto, ApiResources.RobotProperties)
        {
        }
    }
}
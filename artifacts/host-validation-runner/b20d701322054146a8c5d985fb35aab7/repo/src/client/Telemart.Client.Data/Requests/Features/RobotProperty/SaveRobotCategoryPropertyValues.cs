using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.RobotProperty;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class SaveRobotCategoryPropertyValues : CallActionWithBodyRequestResultBase<object, SaveRobotCategoryPropertyValuesDto>
    {
        public SaveRobotCategoryPropertyValues(SaveRobotCategoryPropertyValuesDto dto)
        : base(dto, $"{ApiResources.RobotProperties}/values", "save")
        {
        }
    }
}
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class UpdateCategoryRobotScript : UpdateEntityResultRequestBase<CategoryFullDto, CategoryRobotSaveDto>
    {
        public UpdateCategoryRobotScript(int categoryId, string robotScript, string robotScriptParameters)
            : base(new CategoryRobotSaveDto(categoryId, robotScript, robotScriptParameters), ApiResources.Categories, categoryId, "robot")
        {
        }
    }
}
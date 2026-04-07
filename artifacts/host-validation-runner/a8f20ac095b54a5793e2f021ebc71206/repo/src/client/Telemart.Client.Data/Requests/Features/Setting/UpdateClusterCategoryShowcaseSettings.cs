using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class UpdateClusterCategoryShowcaseSettings : UpdateEntityRequestBase<object, UpdateClusterCategoryShowcaseSettingsDto>
    {
        public UpdateClusterCategoryShowcaseSettings(UpdateClusterCategoryShowcaseSettingsDto dto)
            : base(dto, ApiResources.Settings, "cluster_category_showcase_allow_set_quantity")
        {
        }
    }
}
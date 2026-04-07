using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public class DeleteFeatureGroup : DeleteEntityResultRequestBase<object>
    {
        public DeleteFeatureGroup(int id)
            : base(ApiResources.FeaturesGroups, id)
        {
        }
    }
}
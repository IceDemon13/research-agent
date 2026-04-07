using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureViewParameter : EditorParameter
    {
        public FeatureViewParameter(int featureId, int groupId, int categoryId, int position = 0)
            : base(featureId)
        {
            GroupId = groupId;
            CategoryId = categoryId;
            Position = position;
        }

        public int GroupId { get; }

        public int CategoryId { get; }

        public int Position { get; }
    }
}

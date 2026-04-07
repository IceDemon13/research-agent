using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public sealed class ReorderFeatures : CallEntityActionWithBodyRequestResultBase<object, IReadOnlyCollection<FeaturePositionDto>>
    {
        public ReorderFeatures(int groupId, IReadOnlyCollection<FeaturePositionDto> positions)
            : base(groupId, positions, ApiResources.FeaturesGroups, "reorder")
        {
        }
    }
}
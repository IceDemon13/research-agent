using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public class ReorderFeatureGroups : CallActionWithBodyRequestResultBase<object, IReadOnlyCollection<FeatureGroupPositionDto>>
    {
        public ReorderFeatureGroups(IReadOnlyCollection<FeatureGroupPositionDto> positions)
            : base(positions, ApiResources.FeaturesGroups, "reorder")
        {
        }
    }
}
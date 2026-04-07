using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content.Actions
{
    public sealed class UpdateFeatureValues : CallActionWithBodyRequestResultBase<List<FeatureValueExDto>, IReadOnlyCollection<FeatureValueSaveDto>>
    {
        public UpdateFeatureValues(IReadOnlyCollection<FeatureValueSaveDto> dto)
            : base(dto, ApiResources.Features, "update_values")
        {
        }
    }
}

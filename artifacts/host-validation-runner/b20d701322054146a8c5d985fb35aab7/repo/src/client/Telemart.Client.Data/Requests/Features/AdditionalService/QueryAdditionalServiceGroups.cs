using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class QueryAdditionalServiceGroups : QueryEntitiesRequestBase<AdditionalServiceGroupDto>
    {
        public QueryAdditionalServiceGroups(bool? active = null)
            : base(new AdditionalServiceGroupsFilteringItem(active), ApiResources.AdditionalServicesGroups)
        {
        }

        public sealed class AdditionalServiceGroupsFilteringItem : FilteringItemBase
        {
            public AdditionalServiceGroupsFilteringItem(bool? active)
            {
                Active = active;
            }

            [FilteringItemProperty("active")]
            public bool? Active { get; }
        }
    }
}
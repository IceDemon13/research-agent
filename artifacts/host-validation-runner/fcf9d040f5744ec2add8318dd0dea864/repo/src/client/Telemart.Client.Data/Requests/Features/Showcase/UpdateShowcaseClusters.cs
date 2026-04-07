using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class UpdateShowcaseClusters : CallActionWithBodyRequestResultBase<object, UpdateShowcaseClusters.UpdateShowcaseClustersDto>
    {
        public UpdateShowcaseClusters(IReadOnlyCollection<ShowcaseClusterSaveDto> saveDtos, IReadOnlyCollection<int> showcaseClusterIdsForDelete)
            : base(new UpdateShowcaseClustersDto(saveDtos, showcaseClusterIdsForDelete), ApiResources.Showcases, "clusters")
        {
        }

        public class UpdateShowcaseClustersDto
        {
            public UpdateShowcaseClustersDto(
                IReadOnlyCollection<ShowcaseClusterSaveDto> showcaseClusters,
                IReadOnlyCollection<int> showcaseClusterIdsForDelete)
            {
                ShowcaseClusters = showcaseClusters;
                ShowcaseClusterIdsForDelete = showcaseClusterIdsForDelete;
            }

            [JsonProperty("showcase_clusters")]
            public IReadOnlyCollection<ShowcaseClusterSaveDto> ShowcaseClusters { get; }

            [JsonProperty("delete_showcase_cluster_ids")]
            public IReadOnlyCollection<int> ShowcaseClusterIdsForDelete { get; set; }
        }
    }
}
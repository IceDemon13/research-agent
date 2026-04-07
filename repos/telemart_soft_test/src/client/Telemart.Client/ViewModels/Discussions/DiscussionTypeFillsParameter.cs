using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DiscussionTypeFillsParameter
    {
        public DiscussionTypeFillsParameter(int? discutionTypeId, IReadOnlyCollection<DiscussionTypePropertyItem> propertyItems)
        {
            DiscutionTypeId = discutionTypeId;
            PropertyItems = propertyItems;
        }

        public int? DiscutionTypeId { get; }

        public IReadOnlyCollection<DiscussionTypePropertyItem> PropertyItems { get; }
    }
}
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Audit
{
    public sealed class QueryAuditEntries : QueryEntitiesRequestBase<AuditEntryDto>
    {
        public QueryAuditEntries(string entityTypeName, int? entityId)
            : base(new AuditEntryFilteringItem(entityTypeName, entityId), $"{ApiResources.Audit}/entries")
        {
        }

        private class AuditEntryFilteringItem : IFilteringItem
        {
            public AuditEntryFilteringItem(string entityTypeName, int? entityId)
            {
                EntityTypeName = entityTypeName;
                EntityId = entityId;
            }

            public int? EntityId { get; }

            public string EntityTypeName { get; }

            public IEnumerable<(string, object)> BuildParameters()
            {
                if (!string.IsNullOrWhiteSpace(EntityTypeName))
                {
                    yield return ("entityTypeName", EntityTypeName);
                }

                if (EntityId.HasValue)
                {
                    yield return ("entityId", EntityId.Value.ToString());
                }
            }
        }
    }
}
using System.Collections.Generic;

namespace Telemart.Client.Business.Audit.Property
{
    public sealed class DefaultAuditEntryPropertyProcessor : IAuditEntryPropertyProcessor
    {
        public AuditEntryProperty Process(string propertyName, string oldValue, string newValue, IReadOnlyDictionary<string, (string OldValue, string NewValue)> _)
        {
            return new AuditEntryProperty(propertyName, oldValue, newValue);
        }
    }
}
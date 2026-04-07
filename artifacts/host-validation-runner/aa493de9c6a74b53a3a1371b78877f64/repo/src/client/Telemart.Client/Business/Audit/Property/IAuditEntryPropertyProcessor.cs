using System.Collections.Generic;

namespace Telemart.Client.Business.Audit.Property
{
    public interface IAuditEntryPropertyProcessor
    {
        AuditEntryProperty Process(string propertyName, string oldValue, string newValue, IReadOnlyDictionary<string, (string OldValue, string NewValue)> allProprties);
    }
}
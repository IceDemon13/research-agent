using System.Collections.Generic;

namespace Telemart.Client.Business.Audit.Property
{
    public sealed class TitleAuditEntryPropertyProcessor : IAuditEntryPropertyProcessor
    {
        private readonly string _title;

        public TitleAuditEntryPropertyProcessor(string title)
        {
            _title = title;
        }

        public AuditEntryProperty Process(string propertyName, string oldValue, string newValue, IReadOnlyDictionary<string, (string OldValue, string NewValue)> _)
        {
            return new AuditEntryProperty(_title, oldValue, newValue);
        }
    }
}
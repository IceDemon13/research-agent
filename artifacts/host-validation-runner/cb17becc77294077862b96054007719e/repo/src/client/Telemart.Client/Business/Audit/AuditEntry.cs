using System;

namespace Telemart.Client.Business.Audit
{
    public sealed class AuditEntry
    {
        public AuditEntry(
            int? createdBy,
            DateTime createdOn,
            string propertyName,
            string newValue,
            string oldValue,
            string caption = null)
        {
            CreatedBy = createdBy;
            CreatedOn = createdOn;
            PropertyName = propertyName;
            NewValueFormatted = newValue;
            OldValueFormatted = oldValue;
            Caption = caption;
        }

        public int? CreatedBy { get; }

        public DateTime CreatedOn { get; }

        public string NewValueFormatted { get; }

        public string OldValueFormatted { get; }

        public string PropertyName { get; }

        public string Caption { get; }
    }
}
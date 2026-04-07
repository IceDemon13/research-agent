namespace Telemart.Client.Business.Audit
{
    public struct AuditEntryProperty
    {
        public AuditEntryProperty(string propertyName, string oldValue, string newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string PropertyName { get; }

        public string OldValue { get; }

        public string NewValue { get; }
    }
}
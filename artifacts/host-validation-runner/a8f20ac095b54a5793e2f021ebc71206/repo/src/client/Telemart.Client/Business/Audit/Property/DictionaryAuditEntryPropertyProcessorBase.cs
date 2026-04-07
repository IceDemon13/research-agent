using System;
using System.Collections.Generic;
using DevExpress.Mvvm.Native;

namespace Telemart.Client.Business.Audit.Property
{
    public abstract class DictionaryAuditEntryPropertyProcessorBase<T> : IAuditEntryPropertyProcessor
    {
        private readonly IDictionary<T, string> properties;

        private readonly string title;

        protected DictionaryAuditEntryPropertyProcessorBase(string title, IDictionary<T, string> properties)
        {
            this.title = title;
            this.properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public AuditEntryProperty Process(string propertyName, string oldValue, string newValue, IReadOnlyDictionary<string, (string OldValue, string NewValue)> _)
        {
            T oldValueParsed = ResolveValue(oldValue);
            T newValueParsed = ResolveValue(newValue);

            return new AuditEntryProperty(
                title,
                properties.GetValueOrDefault(oldValueParsed),
                properties.GetValueOrDefault(newValueParsed));
        }

        protected abstract T ResolveValue(string value);
    }
}
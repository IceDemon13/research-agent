using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Audit.Property;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Audit.Entry
{
    public sealed class AuditEntryProcessor : IAuditEntryProcessor
    {
        private readonly Func<AuditEntryDto, string> buildCaption;
        private readonly IAuditEntryPropertyProcessor defaultPropertyProcessor = new DefaultAuditEntryPropertyProcessor();
        private readonly HashSet<string> ignoredProperties;
        private HashSet<(string, string)> ignoredPropertiesByEntryTypeName;
        private readonly IDictionary<string, IAuditEntryPropertyProcessor> propertyProcessors;

        public AuditEntryProcessor(
            IDictionary<string, IAuditEntryPropertyProcessor> propertyProcessors,
            Func<AuditEntryDto, string> buildCaption,
            HashSet<string> ignoredProperties,
            HashSet<(string, string)> ignoredPropertiesByEntryTypeName = null)
        {
            this.propertyProcessors = propertyProcessors;
            this.buildCaption = buildCaption;
            this.ignoredProperties = ignoredProperties;
            this.ignoredPropertiesByEntryTypeName = ignoredPropertiesByEntryTypeName;
        }

        public IEnumerable<AuditEntry> Process(IEnumerable<AuditEntryDto> auditEntries)
        {
            return from e in auditEntries
                   from ep in e.Properties
                   where !ignoredProperties.Contains(ep.PropertyName)
                         && ignoredPropertiesByEntryTypeName?.Contains((e.EntityTypeName, ep.PropertyName)) != true
                         && !string.Equals(ep.OldValueFormatted, ep.NewValueFormatted, StringComparison.Ordinal)
                   orderby e.CreatedOn
                   select CreateAuditEntry(e, ep);
        }

        private AuditEntry CreateAuditEntry(AuditEntryDto entry, AuditEntryPropertyDto entryProperty)
        {
            IAuditEntryPropertyProcessor propertyProcessor = GetPropertyProcessor(entry.EntityTypeName, entryProperty.PropertyName);

            AuditEntryProperty property = propertyProcessor.Process(
                entryProperty.PropertyName,
                entryProperty.OldValueFormatted,
                entryProperty.NewValueFormatted,
                entry.Properties.ToDictionary(x => x.PropertyName, y => (y.OldValueFormatted, y.NewValueFormatted)));

            return new AuditEntry(
                entry.CreatedBy,
                entry.CreatedOn,
                property.PropertyName,
                property.NewValue,
                property.OldValue,
                buildCaption(entry));
        }

        private IAuditEntryPropertyProcessor GetPropertyProcessor(string entityTypeName, string propertyName)
        {
            string fullPropertyName = $"{entityTypeName}.{propertyName}";
            return propertyProcessors.GetValueOrDefault(fullPropertyName, defaultPropertyProcessor);
        }
    }
}
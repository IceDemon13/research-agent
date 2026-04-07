using System.Collections.Generic;

namespace Telemart.Client.Business.Audit.Property
{
    public sealed class StringDictionaryAuditEntryPropertyProcessor : DictionaryAuditEntryPropertyProcessorBase<string>
    {
        public StringDictionaryAuditEntryPropertyProcessor(string title, IDictionary<string, string> properties)
            : base(title, properties)
        {
        }

        protected override string ResolveValue(string value) => value ?? string.Empty;
    }
}

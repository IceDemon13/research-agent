using System;
using System.Collections.Generic;

namespace Telemart.Client.Business.Audit.Property
{
    public sealed class IntDictionaryAuditEntryPropertyProcessor : DictionaryAuditEntryPropertyProcessorBase<int>
    {
        public IntDictionaryAuditEntryPropertyProcessor(string title, IDictionary<int, string> properties)
            : base(title, properties)
        {
        }

        protected override int ResolveValue(string value)
        {
            int parsedValue = 0;

            if (!string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out parsedValue))
            {
                throw new ArgumentException("Error parsing value", nameof(value));
            }

            return parsedValue;
        }
    }
}

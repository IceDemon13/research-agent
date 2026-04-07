using System;
using System.Collections.Generic;
using System.Globalization;


namespace Telemart.Client.Business.Audit.Property
{
    public sealed class DateAuditEntryPropertyProcessor : IAuditEntryPropertyProcessor
    {
        private readonly string dateFormat;

        private readonly string title;

        public DateAuditEntryPropertyProcessor(string title, string dateFormat)
        {
            this.dateFormat = dateFormat;
            this.title = title;
        }

        public AuditEntryProperty Process(string propertyName, string oldValue, string newValue, IReadOnlyDictionary<string, (string OldValue, string NewValue)> _)
        {
            string oldValueStr = string.Empty;
            string newValueStr = string.Empty;

            if (!string.IsNullOrWhiteSpace(oldValue))
            {
                DateTime oldValueParsed;

                if (!DateTime.TryParseExact(oldValue, "M/d/yyyy h:m:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out oldValueParsed)
                    && !DateTime.TryParseExact(oldValue, "M/d/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out oldValueParsed)
                    && !DateTime.TryParse(oldValue, out oldValueParsed))
                {
                    throw new ArgumentException(@"Error parse value", nameof(oldValue));
                }
                else
                {
                    oldValueStr = oldValueParsed.ToString(dateFormat);
                }
            }

            if (!string.IsNullOrWhiteSpace(newValue))
            {
                DateTime newValueParsed;

                if (!DateTime.TryParseExact(newValue, "M/d/yyyy h:m:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out newValueParsed)
                    && !DateTime.TryParseExact(newValue, "M/d/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out newValueParsed)
                    && !DateTime.TryParse(newValue, out newValueParsed))
                {
                    throw new ArgumentException(@"Error parse value", nameof(newValue));
                }
                else
                {
                    newValueStr = newValueParsed.ToString(dateFormat);
                }
            }

            return new AuditEntryProperty(title, oldValueStr, newValueStr);
        }
    }
}
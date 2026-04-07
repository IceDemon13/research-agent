using System.Collections.Generic;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.Business.Audit.Property
{
    public class JsonDictionaryAuditEntryPropertyProcessor<T> : IAuditEntryPropertyProcessor
    {
        private readonly string _jsaonParameter;
        private readonly string _uniteParameter;
        private readonly string _title;
        private readonly IDictionary<T, string> _properties;
        private readonly IDictionary<int, string> _uniteProperties;

        public JsonDictionaryAuditEntryPropertyProcessor(string title, IDictionary<T, string> properties, string jsaonParameter, string uniteParameter, IDictionary<int, string> uniteProperties = null)
        {
            _jsaonParameter = jsaonParameter;
            _uniteParameter = uniteParameter;
            _title = title;
            _properties = properties;
            _uniteProperties = uniteProperties;
        }

        public AuditEntryProperty Process(string propertyName, string oldValue, string newValue, IReadOnlyDictionary<string, (string OldValue, string NewValue)> allProperties)
        {
            T oldValueParsed = ResolveValue(oldValue);
            T newValueParsed = ResolveValue(newValue);

            string oldValueResult = _properties.GetValueOrDefault(oldValueParsed);
            string newValueResult = _properties.GetValueOrDefault(newValueParsed);

            if (!string.IsNullOrEmpty(_uniteParameter))
            {
                (string OldValue, string NewValue)? uniteValue = allProperties?.GetValueOrDefault(_uniteParameter);

                string oldUniteValueResult = uniteValue?.OldValue ?? string.Empty;
                string newUniteValueResult = uniteValue?.NewValue ?? string.Empty;

                if (_uniteProperties?.Count > 0)
                {
                    int.TryParse(uniteValue?.OldValue, out int intUniteOldValue);
                    int.TryParse(uniteValue?.NewValue, out int intUniteNewValue);

                    oldUniteValueResult = _uniteProperties.GetValueOrDefault(intUniteOldValue);
                    newUniteValueResult = _uniteProperties.GetValueOrDefault(intUniteNewValue);
                }

                oldValueResult = !string.IsNullOrEmpty(oldValueResult) ? $"{oldUniteValueResult}, " + oldValueResult : string.Empty;
                newValueResult = !string.IsNullOrEmpty(newValueResult) ? $"{newUniteValueResult}, " + newValueResult : string.Empty;
            }

            return new AuditEntryProperty(
                _title,
                oldValueResult,
                newValueResult);
        }

        private T ResolveValue(string value)
        {
            if (!string.IsNullOrEmpty(value) && JObject.Parse(value).TryGetValue(_jsaonParameter,  out JToken jToken))
            {
                return jToken.ToObject<T>();
            }

            return default;
        }
    }
}
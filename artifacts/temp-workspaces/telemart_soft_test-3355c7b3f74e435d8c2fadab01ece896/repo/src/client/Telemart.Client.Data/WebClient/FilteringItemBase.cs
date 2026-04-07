using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Telemart.Client.Data.WebClient
{
    public abstract class FilteringItemBase : IFilteringItem
    {
        private const string Separator = ",";

        public void WithInitializedDataViaReflection(string serializedParameters)
        {
            (string name, object value)[] deserializedArray = JsonConvert.DeserializeObject<(string nameof, object value)[]>(serializedParameters);

            if (deserializedArray?.Any(x => string.IsNullOrWhiteSpace(x.name)) != false)
            {
                return;
            }

            Dictionary<string, PropertyInfo> propertyNamesDictionary = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => deserializedArray.Select(z => z.name).Contains(x.GetCustomAttribute<FilteringItemPropertyAttribute>()?.Name))
                .ToDictionary(x => x.GetCustomAttribute<FilteringItemPropertyAttribute>().Name);

            foreach ((string name, object value) parameter in deserializedArray)
            {
                if (propertyNamesDictionary.TryGetValue(parameter.name, out PropertyInfo property))
                {
                    if (property.PropertyType == typeof(List<int>))
                    {
                        property.SetValue(this, new List<int>(parameter.value.ToString().Split(',').Select(int.Parse)));
                    }
                    else if (property.PropertyType == typeof(List<string>))
                    {
                        property.SetValue(this, new List<string>(new List<string>(parameter.value.ToString().Split(','))));
                    }
                    else
                    {
                        property.SetValue(this, parameter.value);
                    }
                }
            }
        }

        public IEnumerable<(string, object)> BuildParameters()
        {
            return GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => (x.GetCustomAttribute<FilteringItemPropertyAttribute>()?.Name, GetValue(x.GetValue(this))))
                .Where(x => !string.IsNullOrEmpty(x.Item1) && x.Item2 != null);
        }

        private static object GetValue(object value)
        {
            object result;

            switch (value)
            {
                case string s when !string.IsNullOrWhiteSpace(s):
                case int _:
                case bool _:
                    result = value;
                    break;
                case DateTime d:
                    result = d.ToString("s");
                    break;
                case decimal d:
                    result = d.ToString(CultureInfo.InvariantCulture);
                    break;
                case IReadOnlyCollection<int> c when c.Any():
                    result = string.Join(Separator, c);
                    break;
                case IReadOnlyCollection<string> c when c.Any():
                    result = string.Join(Separator, c);
                    break;
                default:
                    result = null;
                    break;
            }

            return result;
        }
    }
}
using Newtonsoft.Json.Linq;

namespace Telemart.Client.Extensions
{
    public static class JObjectExtensions
    {
        public static bool TryGetValue<T>(this JObject jObject, string propertyName, out T value)
        {
            if (jObject.TryGetValue(propertyName, out JToken jToken))
            {
                value = jToken.ToObject<T>();
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
    }
}

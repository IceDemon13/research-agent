using Newtonsoft.Json;

namespace Telemart.Client.Core.Serialization.Json
{
    public sealed class JsonSerializer<T> : ISerializer<T>
        where T : new()
    {
        public string Serialize(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public T Deserialize(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
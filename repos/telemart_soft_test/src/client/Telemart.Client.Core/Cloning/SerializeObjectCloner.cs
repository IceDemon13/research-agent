using Newtonsoft.Json;

namespace Telemart.Client.Core.Cloning
{
    public static class SerializeObjectCloner
    {
        public static T Clone<T>(T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            string serializedObject = JsonConvert.SerializeObject(source, Formatting.None);
            T deserializedObject = JsonConvert.DeserializeObject<T>(serializedObject);

            return deserializedObject;
        }
    }
}
namespace Telemart.Client.Core.Serialization.Json
{
    public sealed class JsonSerializerBuilder : ISerializerBuilder
    {
        public ISerializer<T> Build<T>()
            where T : new()
        {
            return new JsonSerializer<T>();
        }
    }
}
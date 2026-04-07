namespace Telemart.Client.Core.Serialization
{
    public interface ISerializerBuilder
    {
        ISerializer<T> Build<T>()
            where T : new();
    }
}
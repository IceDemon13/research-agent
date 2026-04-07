namespace Telemart.Client.Core.Serialization
{
    public interface ISerializer<T>
        where T : new()
    {
        string Serialize(T obj);

        T Deserialize(string str);
    }
}
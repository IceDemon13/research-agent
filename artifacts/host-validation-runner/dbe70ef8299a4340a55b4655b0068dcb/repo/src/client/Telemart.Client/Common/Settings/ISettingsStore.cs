using System.Threading.Tasks;

namespace Telemart.Client.Common.Settings
{
    public interface ISettingsStore<T>
        where T : class, new()
    {
        Task<T> LoadAsync();

        Task SaveAsync(T obj);
    }
}
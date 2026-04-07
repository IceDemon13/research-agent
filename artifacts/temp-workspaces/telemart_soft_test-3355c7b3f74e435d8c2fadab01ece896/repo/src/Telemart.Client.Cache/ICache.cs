using System.Linq.Expressions;

namespace Telemart.Client.Cache
{
    public interface ICache
    {
        Task<IEnumerable<T>> GetAllAsync<T>();

        Task<IEnumerable<T>> FindAsync<T>(Expression<Func<T, bool>> predicate, int skip, int take);

        Task UpsertAsync<T>(IEnumerable<T> items);

        Task SetLastSyncDateAsync<T>(DateTime lastSyncDate);

        Task<DateTime?> GetLastSyncDateAsync<T>();

        Task DeleteAllAsync();

        Task DeleteManyAsync<T>(Expression<Func<T, bool>> predicate);
    }
}
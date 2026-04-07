using System.Linq.Expressions;

namespace Telemart.Client.Cache
{
    public class LiteDbCache : ICache
    {
        private readonly ILiteDbConnectionFactory _liteDatabaseFactory;

        public LiteDbCache(ILiteDbConnectionFactory liteDatabaseFactory)
        {
            _liteDatabaseFactory = liteDatabaseFactory;
        }

        public Task<IEnumerable<T>> GetAllAsync<T>()
        {
            return Task.Factory.StartNew(() => _liteDatabaseFactory.Create().GetCollection<T>().Exists(_ => true)
                ? _liteDatabaseFactory.Create().GetCollection<T>().FindAll()
                : null);
        }

        public Task<IEnumerable<T>> FindAsync<T>(Expression<Func<T, bool>> predicate, int skip, int take)
        {
            return Task.Factory.StartNew(x =>
            {
                FindTaskState<T> state = (FindTaskState<T>)x!;
                return _liteDatabaseFactory.Create().GetCollection<T>().Exists(_ => true)
                    ? _liteDatabaseFactory.Create().GetCollection<T>().Find(state.Predicate, state.Skip, state.Take)
                    : null;
            }, new FindTaskState<T>(predicate, skip, take));
        }

        public Task UpsertAsync<T>(IEnumerable<T> items)
        {
            return Task.Factory.StartNew(x =>
            {
                IEnumerable<T> localItems = (IEnumerable<T>)x!;
                return _liteDatabaseFactory.Create().GetCollection<T>().Upsert(localItems);
            }, items);
        }

        public Task SetLastSyncDateAsync<T>(DateTime lastSyncDate)
        {
            string entityName = typeof(T).Name;

            return Task.Factory.StartNew(
                x => _liteDatabaseFactory.Create()
                    .GetCollection<SyncDto>()
                    .Upsert(x as SyncDto), new SyncDto(entityName, lastSyncDate));
        }


        public Task<DateTime?> GetLastSyncDateAsync<T>()
        {
            string id = typeof(T).Name;

            return Task.Factory.StartNew(
                (entityNameLocal) => _liteDatabaseFactory
                    .Create()
                    .GetCollection<SyncDto>()
                    .FindOne(x => x.Id == (entityNameLocal as string))
                    ?.LastSyncDate, id);
        }

        public Task DeleteManyAsync<T>(Expression<Func<T, bool>> predicate)
        {
            return Task.Factory.StartNew(x =>
            {
                Expression<Func<T, bool>> localPredicate = (Expression<Func<T, bool>>)x!;
                
                return _liteDatabaseFactory.Create().GetCollection<T>().DeleteMany(predicate);
            }, predicate);
        }
        
        public Task DeleteAllAsync()
        {
            _liteDatabaseFactory.Disconnect();

            Directory.Delete("db", true);

            return Task.CompletedTask;
        }
    }

    file record FindTaskState<T>(Expression<Func<T, bool>> Predicate, int Skip, int Take);

    internal record SyncDto(string Id, DateTime LastSyncDate);
}
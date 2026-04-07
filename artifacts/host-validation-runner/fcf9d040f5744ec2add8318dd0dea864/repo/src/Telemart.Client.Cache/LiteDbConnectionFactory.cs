using LiteDB;
using Microsoft.Extensions.Logging;

namespace Telemart.Client.Cache
{
    public class LiteDbConnectionFactory : ILiteDbConnectionFactory, IDisposable
    {
        private readonly ILogger<LiteDbConnectionFactory> _logger;

        private LiteDatabase _liteDatabase;

        public LiteDbConnectionFactory(ILogger<LiteDbConnectionFactory> logger)
        {
            _logger = logger;
        }

        public ILiteDatabase Create() => _liteDatabase ??= CreateConnetion(Password);
        public void Disconnect()
        {
            Dispose();
        }

        public string Password
        {
            private get;
            set;
        }

        private LiteDatabase CreateConnetion(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                Disconnect();
                return _liteDatabase;
            }

            if (!Directory.Exists("db"))
            {
                Directory.CreateDirectory("db");
            }

            try
            {
                _liteDatabase?.Dispose();

                _liteDatabase = new LiteDatabase(
                    new ConnectionString("Filename=db/cache.db; Mode=Exclusive; Journal=false; Flush=true; Cache Size=0;")
                    {
                        Password = password
                    });
            }
            catch (LiteException e)
            {
                _liteDatabase?.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                File.Delete(Path.Combine("db", "cache.db"));
                File.Delete(Path.Combine("db", "cache-log.db"));

                _liteDatabase = new LiteDatabase(
                    new ConnectionString("Filename=db/cache.db; Mode=Exclusive; Journal=false; Flush=true; Cache Size=0;")
                    {
                        Password = password
                    });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filed to create LiteDb");
                throw;
            }

            return _liteDatabase;
        }

        public void Dispose()
        {
            _liteDatabase?.Dispose();
            _liteDatabase = null;
        }
    }
}
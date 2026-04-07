using LiteDB;

namespace Telemart.Client.Cache
{
    public interface ILiteDbConnectionFactory
    {
        ILiteDatabase Create();

        void Disconnect();

        string Password { set; }
    }
}
using System.Threading.Tasks;

namespace Telemart.Client.Core.Security
{
    public interface ICryptoManager
    {
        Task<string> DecryptAsync(string chipherText);

        Task<string> EncryptAsync(string clearText);
    }
}
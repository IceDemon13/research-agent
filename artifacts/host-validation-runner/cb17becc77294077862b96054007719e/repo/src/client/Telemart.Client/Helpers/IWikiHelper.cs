using System.Threading.Tasks;

namespace Telemart.Client.Helpers
{
    public interface IWikiHelper
    {
        Task<CookieWiki> GetCookieAutorisationAsync(string url, string email, string password);
    }
}
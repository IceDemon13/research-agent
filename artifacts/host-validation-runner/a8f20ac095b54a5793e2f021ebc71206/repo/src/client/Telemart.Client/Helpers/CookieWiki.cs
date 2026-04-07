using System.Collections.ObjectModel;
using System.Net;

namespace Telemart.Client.Helpers
{
    public sealed class CookieWiki
    {
        public CookieWiki(ReadOnlyCollection<Cookie> cookies)
        {
            Cookies = cookies;
        }

        public ReadOnlyCollection<Cookie> Cookies { get; }
    }
}
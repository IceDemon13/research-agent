using System.Collections.ObjectModel;
using System.Net;

namespace Telemart.Client.Common.Messages
{
    public sealed class SendWikiCookiesMessage
    {
        public SendWikiCookiesMessage(ReadOnlyCollection<Cookie> cookies)
        {
            Cookies = cookies;
        }

        public ReadOnlyCollection<Cookie> Cookies { get; }
    }
}
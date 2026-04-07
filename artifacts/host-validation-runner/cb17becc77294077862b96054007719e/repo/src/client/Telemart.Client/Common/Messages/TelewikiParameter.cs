using System.Collections.ObjectModel;
using System.Net;

namespace Telemart.Client.Common.Messages
{
    public sealed class TelewikiParameter
    {
        public TelewikiParameter(string url, bool isReloadClient = false, ReadOnlyCollection<Cookie> cookies = null)
        {
            Url = url;
            IsReloadClient = isReloadClient;
            Cookies = cookies;
        }

        public string Url { get; }

        public string UserPassword { get; private set; }

        public bool AllowCreate { get; private set; }

        public bool IsReloadClient { get; }

        public ReadOnlyCollection<Cookie> Cookies { get; }

        public void SetUserPassword(string password, bool isCreated)
        {
            UserPassword = password;
            AllowCreate = isCreated;
        }
    }
}
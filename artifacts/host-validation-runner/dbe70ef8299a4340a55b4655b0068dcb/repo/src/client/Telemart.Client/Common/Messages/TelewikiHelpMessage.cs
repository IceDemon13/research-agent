using System.Collections.ObjectModel;
using System.Net;

namespace Telemart.Client.Common.Messages
{
    public sealed class TelewikiHelpMessage
    {
        public TelewikiHelpMessage(string modeleName, string url, bool isReloadClient = false, ReadOnlyCollection<Cookie> cookies = null)
        {
            ModeleName = modeleName;
            TelewikiParameter = new TelewikiParameter(url, isReloadClient, cookies);
        }

        public string ModeleName { get; }

        public TelewikiParameter TelewikiParameter { get; }
    }
}
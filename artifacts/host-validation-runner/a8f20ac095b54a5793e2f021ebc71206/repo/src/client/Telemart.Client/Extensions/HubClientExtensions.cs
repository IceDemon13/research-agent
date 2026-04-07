using System;
using System.Threading.Tasks;
using Telemart.Client.Data.HubClient.Base;

namespace Telemart.Client.Extensions
{
    public static class HubClientExtensions
    {
        public static IHubClientBase<THub> WithHandler<THub, TResponse>(this IHubClientBase<THub> hubClient, string methodName, Func<TResponse, Task> handler)
        where THub : class
        {
            hubClient.RegisterHandler(methodName, handler);

            return hubClient;
        }
    }
}
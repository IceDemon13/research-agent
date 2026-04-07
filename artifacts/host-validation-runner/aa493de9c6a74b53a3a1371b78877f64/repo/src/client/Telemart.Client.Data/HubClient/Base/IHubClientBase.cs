using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Telemart.Client.Data.HubClient.Base
{
    public interface IHubClientBase<THub> : IDisposable, IAsyncDisposable
    where THub : class
    {
        Guid HubClientId { get; }

        HubConnectionState State { get; }

        void RemoveAllHandlersForMethod(string methodName);

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);

        Task SendAsync<TRequest>(string methodName, TRequest request, Action successCallBack = null, CancellationToken cancellationToken = default);

        void RegisterHandler<TResponse>(string methodName, Func<TResponse, Task> handler);

        void OnConnectionClosed(Func<Exception, Task> func);

        void OnReconnecting(Func<Exception, Task> func);

        void OnReconnected(Func<string, Task> func);

        void OnException(Action<Exception> action);
    }
}
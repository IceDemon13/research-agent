using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Telemart.Client.Core.Update;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.HubClient.Hubs;
using Telemart.Client.Data.Options;

namespace Telemart.Client.Data.HubClient.Base
{
    public class HubClientBase<THub> : IHubClientBase<THub>
    where THub : class
    {
        private readonly List<Func<Exception, Task>> _connectionClosedSubscribers;
        private readonly List<Func<Exception, Task>> _reconnectingSubscribers;
        private readonly List<Func<string, Task>> _reconnectedSubscribers;
        private readonly List<Action<Exception>> _exceptionSubscribers;
        private readonly IAuthenticationManager _authenticationManager;
        private readonly IUpdateManager _updateManager;
        private readonly MainServiceOptions _mainServiceOptions;
        private readonly ILogger<HubClientBase<THub>> _logger;
        private HubConnection _connection;

        private event Action<Exception> OnExceptionEvent;

        public HubClientBase(
            MainServiceOptions mainServiceOptions,
            IAuthenticationManager authenticationManager,
            IUpdateManager updateManager,
            ILogger<HubClientBase<THub>> logger)
        {
            _mainServiceOptions = mainServiceOptions;
            _authenticationManager = authenticationManager;
            _updateManager = updateManager;
            _logger = logger;

            _connectionClosedSubscribers = new List<Func<Exception, Task>>();
            _reconnectedSubscribers = new List<Func<string, Task>>();
            _reconnectingSubscribers = new List<Func<Exception, Task>>();
            _exceptionSubscribers = new List<Action<Exception>>();

            HubClientId = Guid.NewGuid();
            BuildConnection();
        }

        public Guid HubClientId { get; }

        public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await ExecuteAsync(() => _connection.StartAsync(cancellationToken));
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
           await ExecuteAsync(() => _connection.StopAsync(cancellationToken));
        }

        public async Task SendAsync<TRequest>(string methodName, TRequest request, Action successCallBack, CancellationToken cancellationToken)
        {
            await ExecuteAsync(() => _connection.SendAsync(methodName, request, cancellationToken), successCallBack);
        }

        public void RegisterHandler<TResponse>(string methodName, Func<TResponse, Task> handler)
        {
            _connection.On(methodName, handler);
        }

        public void RemoveAllHandlersForMethod(string methodName)
        {
            _connection.Remove(methodName);
        }

        public void OnConnectionClosed(Func<Exception, Task> func)
        {
            _connectionClosedSubscribers.Add(func);
            _connection.Closed += func;
        }

        public void OnReconnecting(Func<Exception, Task> func)
        {
            _reconnectingSubscribers.Add(func);
            _connection.Reconnecting += func;
        }

        public void OnReconnected(Func<string, Task> func)
        {
            _reconnectedSubscribers.Add(func);
            _connection.Reconnected += func;
        }

        public void OnException(Action<Exception> action)
        {
            _exceptionSubscribers.Add(action);
            OnExceptionEvent += action;
        }

        public void Dispose()
        {
#pragma warning disable VSTHRD110
#pragma warning disable CS4014
            DisposeAsync();
#pragma warning restore CS4014
#pragma warning restore VSTHRD110
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                foreach (Func<Exception, Task> subscriber in _connectionClosedSubscribers)
                {
                    _connection.Closed -= subscriber;
                }

                foreach (Func<Exception, Task> subscriber in _reconnectingSubscribers)
                {
                    _connection.Reconnecting -= subscriber;
                }

                foreach (Func<string, Task> subscriber in _reconnectedSubscribers)
                {
                    _connection.Reconnected -= subscriber;
                }

                foreach (Action<Exception> subscriber in _exceptionSubscribers)
                {
                    OnExceptionEvent -= subscriber;
                }

                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispose hub connection");
            }
        }

        protected virtual string GetHubName()
        {
            return null;
        }

        private async Task ExecuteAsync(Func<Task> taskFunc, Action successCallBack = null)
        {
            try
            {
                await taskFunc();

                successCallBack?.Invoke();
            }
            catch (Exception ex)
            {
                OnExceptionEvent?.Invoke(ex);
            }
        }

        private void BuildConnection()
        {
            Version currentVersion = _updateManager.GetCurrentVersion();

            _connection = new HubConnectionBuilder()
                .WithUrl($"{_mainServiceOptions.BaseAddress}/api/v1/hub/{GetHubName() ?? throw new InvalidOperationException("hub name cannot be null")}", options =>
                {
                    options.AccessTokenProvider = GetTokenAsync;
                    options.Headers.Add(Headers.UserAgentVersion, currentVersion.ToString());
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .WithAutomaticReconnect(new InfinityRetryPolicy())
                .Build();
        }

        private async Task<string> GetTokenAsync()
        {
            await _authenticationManager.RefreshTokensAsync();
            return _authenticationManager.AccessToken;
        }
    }
}
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.HubClient.Base;

namespace Telemart.Client.Extensions
{
    public static class HubExtensions
    {
        public static IHubClientBase<THub> WithStateHandling<THub>(this IHubClientBase<THub> hubClient, IMessageFacadeService messageFacadeService)
            where THub : class
        {
            hubClient.OnConnectionClosed(OnHubConnectionClosed);

            hubClient.OnReconnecting(OnHubReconnection);

            hubClient.OnReconnected(OnHubReconnected);

            Task OnHubConnectionClosed(Exception ex)
            {
                if (ex is not null)
                {
                    Application.Current.Dispatcher.BeginInvoke(() => { messageFacadeService.ShowNotificationError("Соединение оборвалось с ошибкой"); });
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(() => { messageFacadeService.ShowNotificationError("Соединение оборвалось"); });
                }

                return Task.CompletedTask;
            }

            Task OnHubReconnection(Exception ex)
            {
                if (ex is TimeoutException)
                {
                    Application.Current.Dispatcher.BeginInvoke(() => { messageFacadeService.ShowNotificationWarning("Соединение разорвано, проверьте подключение к интернету"); });
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(() => { messageFacadeService.ShowNotificationWarning("Сервер прервал соединение, попытка восстановления..."); });
                }

                return Task.CompletedTask;
            }

            Task OnHubReconnected(string text)
            {
                Application.Current.Dispatcher.BeginInvoke(() => { messageFacadeService.ShowNotificationInfo("Соединение успешно восстановлено"); });

                return Task.CompletedTask;
            }

            return hubClient;
        }

        public static IHubClientBase<THub> WithExceptionHandling<THub>(this IHubClientBase<THub> hubClient, IMessageFacadeService messageFacadeService)
            where THub : class
        {
            hubClient.OnException(OnHubException);

            return hubClient;

            void OnHubException(Exception ex)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() => { messageFacadeService.ShowNotificationError("Ошибка соединения"); }));
            }
        }
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Notifications;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Notification
{
    public sealed class NotificationsSubscribesViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private readonly TelegramBotOptions _telegramBotOptions;

        private TelemartEnumerableCompareHelper<NotificationSubscribeViewItem> _compareHelper;

        public NotificationsSubscribesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            TelegramBotOptions telegramBotOptions)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _telegramBotOptions = telegramBotOptions;
            OpenTelegramNotificationBotCommand = new AsyncCommand(OpenTelegramNotificationAsync);
            ShowConfigCommand = new DelegateCommand<NotificationSubscribeViewItem>(ShowConfig);
        }

        public ObservableCollection<NotificationSubscribeViewItem> Subscribes
        {
            get { return GetProperty(() => Subscribes); }
            set { SetProperty(() => Subscribes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> NotificationEntities
        {
            get { return GetProperty(() => NotificationEntities); }
            private set { SetProperty(() => NotificationEntities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> NotificationTypes
        {
            get { return GetProperty(() => NotificationTypes); }
            private set { SetProperty(() => NotificationTypes, value); }
        }

        public IAsyncCommand OpenTelegramNotificationBotCommand { get; }

        public IDelegateCommand ShowConfigCommand { get; }

        protected override async Task HandleLoadedAsync()
        {
            NotificationEntities = Dictionaries
                .GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .ToReadOnlyObservableCollection();

            Task<List<NotificationSubscribeDto>> notificationSubscribesTask = WebClient.ExecuteApiRequestAsync(new QueryNotificationSubscribes(WebClient.AuthenticatedEmployee.Id));

            Task<List<NotificationTypeDto>> notificationTypesTask = WebClient.ExecuteApiRequestAsync(new QueryNotificationTypes());

            await Task.WhenAll(notificationSubscribesTask, notificationTypesTask);

            List<NotificationSubscribeDto> subscribes = notificationSubscribesTask.Result;
            List<NotificationTypeDto> notificationTypes = notificationTypesTask.Result;

            foreach (NotificationTypeDto notificationType in notificationTypes)
            {
                bool operationAllowed = notificationType.SubscribeOperationId is null || WebClient.IsOperationAllowed((BusinessOperation)notificationType.SubscribeOperationId.Value);

                if (operationAllowed)
                {
                    if (subscribes.All(x => x.NotificationTypeId != notificationType.Id))
                    {
                        subscribes.Add(new NotificationSubscribeDto()
                        {
                            Id = 0,
                            EmployeeId = WebClient.AuthenticatedEmployee.Id,
                            NotificationTypeId = notificationType.Id,
                            NotificationEntityId = notificationType.EntityId
                        });
                    }
                }
                else
                {
                    NotificationSubscribeDto subscribeToRemove = subscribes.FirstOrDefault(x => x.NotificationTypeId == notificationType.Id);

                    subscribes.Remove(subscribeToRemove);
                }
            }

            Subscribes = subscribes.Select(
                x => new NotificationSubscribeViewItem(
                    x.Id,
                    x.NotificationTypeId,
                    x.NotificationEntityId,
                    x.TargetIds?.Contains(NotificationTarget.Telegram.Id) == true,
                    x.TargetIds?.Contains(NotificationTarget.TelemartClient.Id) == true,
                    x.Config))
                .ToObservableCollection();

            _compareHelper = new TelemartEnumerableCompareHelper<NotificationSubscribeViewItem>(Subscribes);

            NotificationTypes = notificationTypes.Select(x => new ComboBoxItem(x.Id, x.Text.Replace("{documentId}", "[номер]"))).ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Подписка на уведомления";
        }

        protected override async Task HandleOkAsync()
        {
            if (!ValidateBeforeSave())
            {
                return;
            }

            UpdateNotificationSubscribesDto updateDto = new UpdateNotificationSubscribesDto(
                Subscribes.Select(
                    x => new UpdateNotificationSubscribeDto(
                        x.Id,
                        WebClient.AuthenticatedEmployee.Id,
                        x.NotificationTypeId,
                        GetTargetIds(x),
                        x.Config)).Where(x => x.TargetIds?.Any() == true).ToArray());

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateNotificationSubscribes(updateDto)),
                "сохранении подписок",
                "Подписки сохранены",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private static int[] GetTargetIds(NotificationSubscribeViewItem viewItem)
        {
            List<int> targetIds = new List<int>();

            if (viewItem.Telegram)
            {
                targetIds.Add(NotificationTarget.Telegram.Id);
            }

            if (viewItem.TelemartClient)
            {
                targetIds.Add(NotificationTarget.TelemartClient.Id);
            }

            return targetIds.ToArray();
        }

        private bool ValidateBeforeSave()
        {
            if (!_compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return false;
            }

            NotificationSubscribeViewItem orderProductAddedSubscribe = Subscribes.FirstOrDefault(x => x.NotificationTypeId == (int)NotificationType.OrderProductAdded);

            if (orderProductAddedSubscribe != null)
            {
                if ((orderProductAddedSubscribe.TelemartClient || orderProductAddedSubscribe.Telegram)
                    && orderProductAddedSubscribe.Config?.CategoryIds?.Any() != true
                    && orderProductAddedSubscribe.Config?.ProductIds?.Any() != true)
                {
                    MessageFacadeService.ShowNotificationError("Вы активировали подписку на добавление товара,\nно не выбрали ни одной категории или товара");
                    return false;
                }
            }

            NotificationSubscribeViewItem invoiceLateSubscribe = Subscribes.FirstOrDefault(x => x.NotificationTypeId == (int)NotificationType.InvoiceLate);

            if (invoiceLateSubscribe is not null)
            {
                if ((invoiceLateSubscribe.TelemartClient || invoiceLateSubscribe.Telegram)
                    && invoiceLateSubscribe.Config?.InvoiceLateHours is null
                    && invoiceLateSubscribe.Config?.PreorderInvoiceLateHours is null)
                {
                    MessageFacadeService.ShowNotificationError("Вы активировали подписку на задержку накладных,\nно не выбрали время задержки");
                    return false;
                }
            }

            NotificationSubscribeViewItem orderUnconfirmSubscribe = Subscribes.FirstOrDefault(x => x.NotificationTypeId == (int)NotificationType.OrderUnconfirm);

            if (orderUnconfirmSubscribe is not null)
            {
                if ((orderUnconfirmSubscribe.TelemartClient || orderUnconfirmSubscribe.Telegram)
                    && orderUnconfirmSubscribe.Config?.ContractorIds?.Any() != true)
                {
                    MessageFacadeService.ShowNotificationError("Вы активировали подписку на рассогласование заказа, но не выбрали контрагента");
                    return false;
                }
            }

            NotificationSubscribeViewItem ordersReadyToPackSubscribe = Subscribes.FirstOrDefault(x => x.NotificationTypeId == (int)NotificationType.OrdersReadyToPack);

            if (ordersReadyToPackSubscribe is not null)
            {
                if ((ordersReadyToPackSubscribe.TelemartClient || ordersReadyToPackSubscribe.Telegram)
                    && (ordersReadyToPackSubscribe.Config?.CarryIds?.Any() != true || ordersReadyToPackSubscribe.Config?.WarehouseIds?.Any() != true))
                {
                    MessageFacadeService.ShowNotificationError("Вы активировали подписку на готовность к упаковке, но не настроили фильтры");
                    return false;
                }
            }

            return true;
        }

        private async Task OpenTelegramNotificationAsync()
        {
            EmployeeRichDto employee =
                await WebClient.ExecuteApiRequestAsync(new QueryEmployee(WebClient.AuthenticatedEmployee.Id));

            if (string.IsNullOrEmpty(employee.Telegram))
            {
                MessageFacadeService.ShowNotificationError("У вашего пользователя не заполнен Telegram аккаунт");
                return;
            }

            ProcessHelper.Start($"https://t.me/{_telegramBotOptions.NotificationsBotName}");
        }

        private void ShowConfig(NotificationSubscribeViewItem selectedSubscribe)
        {
            switch (selectedSubscribe.NotificationTypeId)
            {
                case (int)NotificationType.OrderProductAdded:
                    ShowConfigInternal<AddOrderProductNotificationsViewModel>();
                    break;
                case (int)NotificationType.InvoiceLate:
                    ShowConfigInternal<InvoiceLateNotificationsViewModel>();
                    break;
                case (int)NotificationType.OrderUnconfirm:
                    ShowConfigInternal<OrderUnconfirmNotificationsViewModel>();
                    break;
                case (int)NotificationType.OrdersReadyToPack:
                    ShowConfigInternal<OrdersReadyToPackNotificationsViewModel>();
                    break;
            }

            void ShowConfigInternal<TViewModel>()
                where TViewModel : TelemartDialogViewModelBase, INotificationConfigViewModel
            {
                TViewModel viewModel = DialogDocumentManagerService.ShowView<TViewModel>(selectedSubscribe.Config, this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                selectedSubscribe.Config = viewModel.Config;
            }
        }
    }
}
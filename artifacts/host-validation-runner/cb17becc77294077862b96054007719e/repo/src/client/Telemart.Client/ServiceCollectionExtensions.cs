using System;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telemart.Client.Common.Services;
using Telemart.Client.Data;
using Telemart.Client.Data.Options;
using Telemart.Client.Oktell;
using Telemart.Client.ViewModels;

namespace Telemart.Client
{
    public static class ServiceCollectionExtensions
    {
        public static ICallServiceClient GetCallClient(IServiceProvider serviceProvider)
        {
            IOptionsMonitor<CallTrackOptions> options = serviceProvider.GetRequiredService<IOptionsMonitor<CallTrackOptions>>();

            if (options.CurrentValue.Type == CallTrackType.Asterisk)
            {
                return serviceProvider.GetRequiredService<AsteriskClient>();
            }

            return serviceProvider.GetRequiredService<OktellClient>();
        }

        public static INotificationService CreateNotificationService()
        {
            INotificationService notificationService = new NotificationService
            {
                ApplicationId = "telemart.client",
                UseWin8NotificationsIfAvailable = false,
                CustomNotificationPosition = NotificationPosition.BottomRight,
                CustomNotificationVisibleMaxCount = 5,
                CustomNotificationDuration = TimeSpan.FromSeconds(3),
                CustomNotificationTemplateSelector = new CustomNotificationTemplateSelector()
            };

            return notificationService;
        }

        public static INewCommentNotificationService CreateNewCommentNotificationService()
        {
            INewCommentNotificationService notificationService = new NewCommentNotificationService()
            {
                ApplicationId = "telemart.client",
                UseWin8NotificationsIfAvailable = false,
                CustomNotificationPosition = NotificationPosition.BottomRight,
                CustomNotificationVisibleMaxCount = 5,
                CustomNotificationDuration = TimeSpan.FromSeconds(60),
                CustomNotificationTemplateSelector = new CustomNewCommentNotificationTemplateSelector()
            };

            return notificationService;
        }

        public static IEntityNotificationService CreateEntityNotificationService()
        {
            IEntityNotificationService notificationService = new EntityNotificationService()
            {
                ApplicationId = "telemart.client",
                UseWin8NotificationsIfAvailable = false,
                CustomNotificationPosition = NotificationPosition.BottomRight,
                CustomNotificationVisibleMaxCount = 5,
                CustomNotificationDuration = TimeSpan.FromDays(1),
                CustomNotificationTemplateSelector = new EntityNotificationTemplateSelector()
            };

            return notificationService;
        }
    }
}
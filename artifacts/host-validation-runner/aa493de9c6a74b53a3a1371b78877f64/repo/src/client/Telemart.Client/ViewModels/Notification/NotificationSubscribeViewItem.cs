using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public class NotificationSubscribeViewItem : TelemartCloneableViewItemBase
    {
        public NotificationSubscribeViewItem(
            int id,
            int notificationTypeId,
            int? notificationEntityId,
            bool telegram,
            bool telemartClient,
            NotificationSubscribeConfigDto config)
        {
            Config = config;
            Id = id;
            NotificationTypeId = notificationTypeId;
            NotificationEntityId = notificationEntityId;
            Telegram = telegram;
            TelemartClient = telemartClient;
            ConfigButtonVisible = NotificationTypeId
                is (int)NotificationType.OrderProductAdded
                or (int)NotificationType.InvoiceLate
                or (int)NotificationType.OrderUnconfirm
                or (int)NotificationType.OrdersReadyToPack;
        }

        public NotificationSubscribeViewItem()
        {
        }

        public int NotificationTypeId
        {
            get { return GetProperty(() => NotificationTypeId); }
            init { SetProperty(() => NotificationTypeId, value); }
        }

        public int? NotificationEntityId
        {
            get { return GetProperty(() => NotificationEntityId); }
            init { SetProperty(() => NotificationEntityId, value); }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            init { SetProperty(() => Id, value); }
        }

        public bool Telegram
        {
            get { return GetProperty(() => Telegram); }
            set { SetProperty(() => Telegram, value); }
        }

        public bool TelemartClient
        {
            get { return GetProperty(() => TelemartClient); }
            set { SetProperty(() => TelemartClient, value); }
        }

        public NotificationSubscribeConfigDto Config
        {
            get { return GetProperty(() => Config); }
            set { SetProperty(() => Config, value); }
        }

        public bool ConfigButtonVisible
        {
            get { return GetProperty(() => ConfigButtonVisible); }
            private set { SetProperty(() => ConfigButtonVisible, value); }
        }

        public override object Clone()
        {
            NotificationSubscribeViewItem item = (NotificationSubscribeViewItem)base.Clone();

            if (Config is null)
            {
                item.Config = null;
            }
            else
            {
                item.Config = new NotificationSubscribeConfigDto()
                {
                    CategoryIds = Config.CategoryIds,
                    ProductIds = Config.ProductIds,
                };
            }

            return item;
        }
    }
}
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.AspNetCore.SignalR.Client;
using Telemart.Client.Common.HubFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Data.HubClient.Hubs;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public sealed class NotifyAllViewModel : TelemartDialogViewModelBase
    {
        private readonly IHubClientFactory hubClientFactory;

        public NotifyAllViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IHubClientFactory hubClientFactory)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.hubClientFactory = hubClientFactory;
        }

        public ReadOnlyObservableCollection<NotificationImage> NotificationImages
        {
            get { return GetProperty(() => NotificationImages); }
            set { SetProperty(() => NotificationImages, value); }
        }

        public NotificationImage SelectedNotificationImage
        {
            get { return GetProperty(() => SelectedNotificationImage); }
            set { SetProperty(() => SelectedNotificationImage, value); }
        }

        public string Header
        {
            get { return GetProperty(() => Header); }
            set { SetProperty(() => Header, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }

        public static void BuildMetadata(MetadataBuilder<NotifyAllViewModel> builder)
        {
            builder.Property(x => x.Text)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Header)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedNotificationImage).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            NotificationImages = Dictionaries.GetItems<NotificationImage>().ToReadOnlyObservableCollection();

            Title = "Создание уведомления";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            NotifyAllDto dto = new()
            {
                Header = Header,
                Text = Text,
                NotificationImageId = SelectedNotificationImage.Id,
                NotificationFormatId = (int)NotificationFormat.MessageBox
            };

            bool closeView = false;

            await using (IHubClientBase<NotificationHub> notificationHubClient = hubClientFactory.Create<NotificationHub>(MessageFacadeService))
            {
                await notificationHubClient.StartAsync();

                if (notificationHubClient.State == HubConnectionState.Connected)
                {
                    await notificationHubClient.SendAsync(NotificationHub.NotifyAllMethod, dto, () => MessageFacadeService.ShowNotificationInfo("Уведомление успешно отправлено"));
                    closeView = true;
                }
            }

            if (closeView)
            {
                CloseOk();
            }
        }
    }
}
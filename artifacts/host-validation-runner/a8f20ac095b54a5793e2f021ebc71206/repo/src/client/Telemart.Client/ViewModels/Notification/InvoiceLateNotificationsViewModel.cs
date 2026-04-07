using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public sealed class InvoiceLateNotificationsViewModel : TelemartDialogViewModelBase, INotificationConfigViewModel
    {
        public InvoiceLateNotificationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public NotificationSubscribeConfigDto Config { get; set; }

        public int? InvoiceLateHours
        {
            get { return GetProperty(() => InvoiceLateHours); }
            set { SetProperty(() => InvoiceLateHours, value); }
        }

        public int? PreorderInvoiceLateHours
        {
            get { return GetProperty(() => PreorderInvoiceLateHours); }
            set { SetProperty(() => PreorderInvoiceLateHours, value); }
        }

        public static void BuildMetadata(MetadataBuilder<InvoiceLateNotificationsViewModel> builder)
        {
            builder.Property(x => x.PreorderInvoiceLateHours)
                .MatchesRule(
                    x => x is null or > 0 and < 1000,
                    () => "Значение должно быть в диапазоне 1..999");

            builder.Property(x => x.InvoiceLateHours)
                .MatchesRule(
                    x => x is null or > 0 and < 200,
                    () => "Значение должно быть в диапазоне 1..199");
        }

        protected override Task HandleLoadedAsync()
        {
            Config = (NotificationSubscribeConfigDto)Parameter;

            Config ??= new NotificationSubscribeConfigDto();

            InvoiceLateHours = Config.InvoiceLateHours;
            PreorderInvoiceLateHours = Config.PreorderInvoiceLateHours;

            Title = "Уведомление о задержке накладных";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            Config.InvoiceLateHours = InvoiceLateHours;
            Config.PreorderInvoiceLateHours = PreorderInvoiceLateHours;

            CloseOk();
            return Task.CompletedTask;
        }
    }
}
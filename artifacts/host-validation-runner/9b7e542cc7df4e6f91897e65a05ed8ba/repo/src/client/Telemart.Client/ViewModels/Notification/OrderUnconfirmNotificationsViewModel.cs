using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public class OrderUnconfirmNotificationsViewModel : TelemartDialogViewModelBase, INotificationConfigViewModel
    {
        public OrderUnconfirmNotificationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ObservableCollection<int> SelectedContractorIds
        {
            get { return GetProperty(() => SelectedContractorIds); }
            set { SetProperty(() => SelectedContractorIds, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public static void BuildMetadata(MetadataBuilder<OrderUnconfirmNotificationsViewModel> builder)
        {
            builder.Property(x => x.SelectedContractorIds)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<ContractorDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(true), true);

            Contractors = pagedResult.Data
                .Where(x => x.IsFolder == false && x.IsClient && x.Active)
                .OrderBy(x => x.SubdivisionId)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Config = (NotificationSubscribeConfigDto)Parameter;

            Config ??= new NotificationSubscribeConfigDto();

            SelectedContractorIds = Config.ContractorIds?.ToObservableCollection();

            Title = "Уведомление о рассогласовании заказа";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            Config.ContractorIds = SelectedContractorIds.ToArray();

            CloseOk();
            return Task.CompletedTask;
        }

        public NotificationSubscribeConfigDto Config { get; set; }
    }
}
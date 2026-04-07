using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public sealed class OrdersReadyToPackNotificationsViewModel : TelemartDialogViewModelBase, INotificationConfigViewModel
    {
        public OrdersReadyToPackNotificationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ObservableCollection<int> SelectedCarryIds
        {
            get { return GetProperty(() => SelectedCarryIds); }
            set { SetProperty(() => SelectedCarryIds, value); }
        }

        public ObservableCollection<int> SelectedWarehouseIds
        {
            get { return GetProperty(() => SelectedWarehouseIds); }
            set { SetProperty(() => SelectedWarehouseIds, value); }
        }

        public ObservableCollection<int> SelectedContractorIds
        {
            get { return GetProperty(() => SelectedContractorIds); }
            set { SetProperty(() => SelectedContractorIds, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public static void BuildMetadata(MetadataBuilder<OrdersReadyToPackNotificationsViewModel> builder)
        {
            builder.Property(x => x.SelectedContractorIds)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedCarryIds)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<ContractorDto> contractorsData = await WebClient.ExecuteApiRequestAsync(new QueryContractors(true), true);

            Contractors = contractorsData.Data
                .Where(x => x.IsFolder == false && x.IsClient && x.Active)
                .OrderBy(x => x.SubdivisionId)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            PagedResult<WarehouseDto> warehousesData = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehousesData.Data
                .Where(x => x.Active > 0 && x.TypeId is WarehouseKind.MainId or WarehouseKind.PickupId or WarehouseKind.ShowCaseId)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Carries = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Config = (NotificationSubscribeConfigDto)Parameter;

            Config ??= new NotificationSubscribeConfigDto();

            SelectedContractorIds = Config.ContractorIds?.ToObservableCollection();
            SelectedCarryIds = Config.CarryIds?.ToObservableCollection();
            SelectedWarehouseIds = Config.WarehouseIds?.ToObservableCollection();

            Title = "Уведомление о готовности заказов к упаковке";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            Config.ContractorIds = SelectedContractorIds?.ToArray();
            Config.CarryIds = SelectedCarryIds?.ToArray();
            Config.WarehouseIds = SelectedWarehouseIds?.ToArray();

            CloseOk();
            return Task.CompletedTask;
        }

        public NotificationSubscribeConfigDto Config { get; set; }
    }
}
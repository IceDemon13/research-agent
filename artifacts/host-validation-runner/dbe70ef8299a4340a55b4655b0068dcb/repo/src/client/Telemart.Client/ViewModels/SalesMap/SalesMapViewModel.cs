using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Map;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.SalesMap;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.SalesMap
{
    public sealed class SalesMapViewModel : TelemartViewModelBase
    {
        public SalesMapViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            RefreshCommand = new AsyncCommand(RefreshAsync);
        }

        public IDelegateCommand ResetFilterCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public List<SalesMapDto> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public BingMapKind AppliedMapKind
        {
            get { return GetProperty(() => AppliedMapKind); }
            set { SetProperty(() => AppliedMapKind, value); }
        }

        public Tuple<string, BingMapKind> SelectedMapKind
        {
            get { return GetProperty(() => SelectedMapKind); }
            set { SetProperty(() => SelectedMapKind, value); }
        }

        public ReadOnlyObservableCollection<Tuple<string, BingMapKind>> MapKinds
        {
            get { return GetProperty(() => MapKinds); }
            private set { SetProperty(() => MapKinds, value); }
        }

        public DateTime? DeliveryTimeAfter
        {
            get { return GetProperty(() => DeliveryTimeAfter); }
            set { SetProperty(() => DeliveryTimeAfter, value); }
        }

        public DateTime? DeliveryTimeBefore
        {
            get { return GetProperty(() => DeliveryTimeBefore); }
            set { SetProperty(() => DeliveryTimeBefore, value); }
        }

        public ReadOnlyObservableCollection<CarryType> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public List<object> SelectedCarries
        {
            get { return GetProperty(() => SelectedCarries); }
            set { SetProperty(() => SelectedCarries, value); }
        }

        public bool IsSearchPanelOpened
        {
            get { return GetProperty(() => IsSearchPanelOpened); }
            set { SetProperty(() => IsSearchPanelOpened, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            Carries = Dictionaries.GetItems<CarryType>()
                .Where(x => new[]
                {
                    CarryType.PickupId,
                    CarryType.NpWarehouseId,
                    CarryType.NpPostBoxId,
                    CarryType.NpDeliveryId,
                    CarryType.MeWarehouseId,
                    CarryType.MePostBoxId,
                    CarryType.MeMiniWarehouseId,
                    CarryType.MeDeliveryId,
                    CarryType.UpWarehouseId,
                    CarryType.UpDeliveryId,
                    CarryType.DeliveryId,
                    CarryType.LocalExpressId,
                    CarryType.SmartPostId,
                    CarryType.KievDeliveryId,
                    CarryType.GabaritkaId
                }.Contains(x.Id))
                .ToReadOnlyObservableCollection();

            MapKinds = new[]
                {
                    ("Спутник + дороги", BingMapKind.Hybrid).ToTuple(),
                    ("Спутник", BingMapKind.Area).ToTuple(),
                    ("Дороги", BingMapKind.Road).ToTuple(),
                    ("Дороги (темный)", BingMapKind.RoadDark).ToTuple(),
                    ("Дороги (серый)", BingMapKind.RoadGray).ToTuple(),
                    ("Дороги (светлый)", BingMapKind.RoadLight).ToTuple()
                }
                .ToReadOnlyObservableCollection();

            IsSearchPanelOpened = true;
            ResetFilterCommand.Execute(null);

            return base.HandleLoadedAsync();
        }

        private void ResetFilter()
        {
            SelectedCarries = Carries.Cast<object>().ToList();

            DeliveryTimeAfter = DateTime.Today.AddMonths(-1);
            DeliveryTimeBefore = DateTime.Today;

            SelectedMapKind = MapKinds.First();

            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            AppliedMapKind = SelectedMapKind.Item2;

            try
            {
                Items = await WebClient.ExecuteApiRequestAsync(new QuerySalesMaps(GetFilteringItem()));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to query sales map");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
            }
        }

        private IFilteringItem GetFilteringItem()
        {
            return new SalesMapFilteringItem(
                DeliveryTimeAfter,
                DeliveryTimeBefore,
                SelectedCarries?.Cast<CarryType>().Select(x => x.Id).ToArray());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ScheduleDeliveryCityCarryViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyCollection<CityDto> allActiveCities;

        public ScheduleDeliveryCityCarryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public int? SelectedCarryId
        {
            get { return GetProperty(() => SelectedCarryId); }
            set { SetProperty(() => SelectedCarryId, value, SelectedCarryIdChanged); }
        }

        public int? SelectedCityId
        {
            get { return GetProperty(() => SelectedCityId); }
            set { SetProperty(() => SelectedCityId, value); }
        }

        public DateOnly? SelectedDate
        {
            get { return GetProperty(() => SelectedDate); }
            set { SetProperty(() => SelectedDate, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public IReadOnlyCollection<OrderDto> Orders
        {
            get { return GetProperty(() => Orders); }
            private set { SetProperty(() => Orders, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ScheduleDeliveryCityCarryViewModel> builder)
        {
            builder.Property(x => x.SelectedCarryId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedDate).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true);

            Carries = Dictionaries
                .GetItems<CarryType>()
                .Where(x => x.CarryProviderId == CarryProvider.TelemartId && x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            HashSet<int> carryIds = Carries.Select(x => x.Id).ToHashSet();

            allActiveCities = cities.Data
                .Where(x => x.Active && x.CityCarries.Any(z => carryIds.Contains(z.CarryId)))
                .ToArray();

            await base.HandleLoadedAsync();

            Title = "Массовое планирование доставки заказов";
        }

        protected override async Task HandleOkAsync()
        {
            OrderFilteringItem ordersFilteringItem = new OrderFilteringItem()
            {
                Carries = new List<int>()
                {
                    SelectedCarryId!.Value
                },
                Cities = new List<int>()
                {
                    SelectedCityId!.Value
                },
                OrderStatuses = new List<int>()
                {
                    OrderStatus.Packed.Id,
                    OrderStatus.Confirmed.Id
                }
            };

            PagedResult<OrderDto> orders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(ordersFilteringItem));

            if (!orders.Data.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет заказов для планирования");
                return;
            }

            Orders = orders.Data;

            CloseOk();
        }

        private void SelectedCarryIdChanged()
        {
            if (SelectedCarryId is null)
            {
                return;
            }

            Cities = allActiveCities.Where(x => x.CityCarries.Any(z => z.CarryId == SelectedCarryId.Value))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            if (Cities.All(x => x.Id != SelectedCityId))
            {
                SelectedCityId = null;
            }
        }
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class SelectMovementsViewModel : TelemartDialogViewModelBase
    {
        public SelectMovementsViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SelectedMovements = new ObservableCollection<SelectMovementViewItem>();
        }
        #region DialogSettings

        public override int Width => 650;

        public override int MinWidth => 600;

        public override int MaxWidth => 800;

        public override int Height => 500;

        public override int MinHeight => 450;

        public override int MaxHeight => 550;

        #endregion

        public ObservableCollection<SelectMovementViewItem> Movements
        {
            get { return GetProperty(() => Movements); }
            private set { SetProperty(() => Movements, value); }
        }

        public ObservableCollection<SelectMovementViewItem> SelectedMovements
        {
            get { return GetProperty(() => SelectedMovements); }
            set { SetProperty(() => SelectedMovements, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            SelectMovementsParameter parameter = (SelectMovementsParameter)Parameter;

            (PagedResult<WarehouseDto> warehousesPagedResult, List<LocationEntityDto> locations) result = await TaskExt
                .WhenAll(WebClient.ExecuteApiRequestAsync(new QueryWarehouses()), WebClient.ExecuteApiRequestAsync(new QueryLocations()));

            Dictionary<int, WarehouseDto> warehousesDictionary = result.warehousesPagedResult.Data.ToDictionary(x => x.Id);
            var locationsDictionary = result.locations.ToDictionary(x => x.Id, x => x.Name);

            Movements = new ObservableCollection<SelectMovementViewItem>(
                parameter.MovementsToSelect.Select(x => new SelectMovementViewItem
                {
                    Id = x.Id,
                    Name = $"{warehousesDictionary[x.WarehouseFromId].Name} => {warehousesDictionary[x.WarehouseToId].Name}",
                    ToLocationName = locationsDictionary.GetValueOrDefault(warehousesDictionary[x.WarehouseToId].LocationId ?? 0)
                }));

            await base.HandleLoadedAsync();

            Title = parameter.Title;
        }

        protected override Task HandleOkAsync()
        {
            if (!SelectedMovements.Any())
            {
                MessageFacadeService.ShowNotificationError("Ничего не выбрано");
                return Task.CompletedTask;
            }

            if (SelectedMovements.GroupBy(x => x.ToLocationName).Count() > 1)
            {
                MessageFacadeService.ShowNotificationError("Нельзя выбирать перемещения в разные магазины");
                return Task.CompletedTask;
            }

            if (SelectedMovements.Count == 1)
            {
                MessageFacadeService.ShowNotificationError("Должно быть выбрано больше одного перемещения");
                return Task.CompletedTask;
            }

            CloseOk();
            return Task.CompletedTask;
        }
    }
}
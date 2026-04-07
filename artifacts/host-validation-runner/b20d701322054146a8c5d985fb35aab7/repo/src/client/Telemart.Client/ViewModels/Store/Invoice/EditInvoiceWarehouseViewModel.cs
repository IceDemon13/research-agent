using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class EditInvoiceWarehouseViewModel : TelemartDialogViewModelBase
    {
        private int _warehouseId;

        public EditInvoiceWarehouseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int OldWarehouseId
        {
            get { return GetProperty(() => OldWarehouseId); }
            set { SetProperty(() => OldWarehouseId, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        public int GetSelectedWarehouseId()
        {
            return _warehouseId;
        }

        protected override async Task HandleLoadedAsync()
        {
            if (Parameter is EditInvoiceWarehouseParameter parameter)
            {
                OldWarehouseId = parameter.WarehouseId;
            }

            await LoadWarehousesAsync();

            Title = "Изменение склада";
        }

        protected override Task HandleOkAsync()
        {
            if (WarehouseId == OldWarehouseId)
            {
                MessageFacadeService.ShowNotificationWarning("Вы выбрали текущий склад");

                return Task.CompletedTask;
            }

            _warehouseId = WarehouseId ?? 0;

            CloseOk();

            return Task.CompletedTask;
        }

        private async Task LoadWarehousesAsync()
        {
            List<WarehouseDto> warehouseDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync(),
                "получении списка складов",
                null,
                this,
                true,
                showNotification: false);

            Warehouses = warehouseDtos
                .Where(warehouse => warehouse.Active == 1 && (warehouse.TypeId == WarehouseKind.Main.Id
                                                              || warehouse.TypeId == WarehouseKind.Pickup.Id
                                                              || warehouse.TypeId == WarehouseKind.ShowCase.Id
                                                              || warehouse.TypeId == WarehouseKind.Service.Id
                                                              || warehouse.TypeId == WarehouseKind.Assembly.Id))
                .OrderBy(x => x.Name)
                .Select(y => new ComboBoxItem(y.Id, y.Name))
                .ToReadOnlyObservableCollection();
        }
    }
}
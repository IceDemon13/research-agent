using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseCopyViewModel : TelemartDialogViewModelBase
    {
        public WarehouseCopyViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        #region INPC

        public int? SelectedWarehouseId
        {
            get { return GetProperty(() => SelectedWarehouseId); }
            set { SetProperty(() => SelectedWarehouseId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CopyChoices
        {
            get { return GetProperty(() => CopyChoices); }
            set { SetProperty(() => CopyChoices, value); }
        }

        public bool IsCopyMovements
        {
            get { return GetProperty(() => IsCopyMovements); }
            set { SetProperty(() => IsCopyMovements, value, () => RaisePropertyChanged(nameof(SelectedMovementsCopyChoice))); }
        }

        public bool IsCopyDeliveries
        {
            get { return GetProperty(() => IsCopyDeliveries); }
            set { SetProperty(() => IsCopyDeliveries, value, () => RaisePropertyChanged(nameof(SelectedDeliveriesCopyChoice))); }
        }

        public bool IsCopyPerfomances
        {
            get { return GetProperty(() => IsCopyPerfomances); }
            set { SetProperty(() => IsCopyPerfomances, value, () => RaisePropertyChanged(nameof(SelectedPerfomancesCopyChoice))); }
        }

        public int? SelectedMovementsCopyChoice
        {
            get { return GetProperty(() => SelectedMovementsCopyChoice); }
            set { SetProperty(() => SelectedMovementsCopyChoice, value); }
        }

        public int? SelectedDeliveriesCopyChoice
        {
            get { return GetProperty(() => SelectedDeliveriesCopyChoice); }
            set { SetProperty(() => SelectedDeliveriesCopyChoice, value); }
        }

        public int? SelectedPerfomancesCopyChoice
        {
            get { return GetProperty(() => SelectedPerfomancesCopyChoice); }
            set { SetProperty(() => SelectedPerfomancesCopyChoice, value); }
        }

        public bool CanCopyPerfomance
        {
            get { return GetProperty(() => CanCopyPerfomance); }
            private set { SetProperty(() => CanCopyPerfomance, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<WarehouseCopyViewModel> builder)
        {
            builder.Property(x => x.SelectedWarehouseId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedDeliveriesCopyChoice)
                .MatchesInstanceRule((x, y) => x.HasValue || !y.IsCopyDeliveries, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedMovementsCopyChoice)
                .MatchesInstanceRule((x, y) => x.HasValue || !y.IsCopyMovements, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedPerfomancesCopyChoice)
                .MatchesInstanceRule((x, y) => x.HasValue || !y.IsCopyPerfomances, () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            CanCopyPerfomance = WebClient.IsOperationAllowed(BusinessOperation.WarehousePerfomanceCreate) && WebClient.IsOperationAllowed(BusinessOperation.WarehousePerfomanceUpdate);

            CopyChoices = Dictionaries.GetItems<CopyActionType>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            WarehouseCopyParameter parameter = (WarehouseCopyParameter) Parameter;

            await LoadWarehouseAsync(parameter.WarehouseId);

            Title = "Копирование настроек склада";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        protected override bool CanOk()
        {
            return SelectedWarehouseId.HasValue
                   && ((IsCopyDeliveries && SelectedDeliveriesCopyChoice.HasValue)
                       || (IsCopyMovements && SelectedMovementsCopyChoice.HasValue)
                       || (IsCopyPerfomances && SelectedPerfomancesCopyChoice.HasValue));
        }

        private async Task LoadWarehouseAsync(int warehouseId)
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses());

            Warehouses = warehouses.Data.Where(x => x.Active > 0 && x.Id != warehouseId).OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }
    }
}
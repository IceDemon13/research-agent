using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class GetServiceProductWarehouseViewModel : TelemartDialogViewModelBase
    {
        private int[] needWarehouseTypeIds = { WarehouseKind.Pickup.Id, WarehouseKind.Main.Id, WarehouseKind.ShowCase.Id, WarehouseKind.Assembly.Id };

        public GetServiceProductWarehouseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public GetServiceProductWarehouseViewModel()
        {
        }

        public ObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public int? SelectedWarehouseId
        {
            get { return GetProperty(() => SelectedWarehouseId); }
            set { SetProperty(() => SelectedWarehouseId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<GetServiceProductWarehouseViewModel> builder)
        {
            builder.Property(x => x.SelectedWarehouseId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehouses.Data
                .Where(x => needWarehouseTypeIds.Contains(x.TypeId) && x.Active == 1)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();

            Title = "Выберите склад получатель";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}
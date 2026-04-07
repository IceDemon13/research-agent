using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListChoiceViewModel : TelemartDialogViewModelBase
    {
        public PackListChoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PackLists
        {
            get { return GetProperty(() => PackLists); }
            set { SetProperty(() => PackLists, value); }
        }

        public int? SelectedPackListId
        {
            get { return GetProperty(() => SelectedPackListId); }
            set { SetProperty(() => SelectedPackListId, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public string NameWarehouse
        {
            get { return GetProperty(() => NameWarehouse); }
            set { SetProperty(() => NameWarehouse, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PackListChoiceViewModel> builder)
        {
            builder.Property(x => x.SelectedPackListId)
                .MatchesRule(x => x.HasValue, () => "Выберите лист на сборку");
        }

        public int GetPackList()
        {
            return SelectedPackListId ?? 0;
        }

        protected override async Task HandleLoadedAsync()
        {
            PackListChoiceParameter parameter = (PackListChoiceParameter)Parameter;

            WarehouseDto warehouseDto = await WebClient.ExecuteApiRequestAsync(new QueryWarehouse(parameter.WarehouseId));

            WarehouseId = parameter.WarehouseId;

            NameWarehouse = warehouseDto?.Name;

            List<PackListDto> packListDtos = await WebClient.ExecuteApiRequestAsync(new QueryInProgressPackLists(new PackListsInProgressFilteringItem(parameter.WarehouseId)));

            PackLists = packListDtos?
                .Select(x => new ComboBoxItem(x.Id, x.Id.ToString()))
                .ToReadOnlyObservableCollection();

            Title = "Выберите лист на сборку";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }
    }
}
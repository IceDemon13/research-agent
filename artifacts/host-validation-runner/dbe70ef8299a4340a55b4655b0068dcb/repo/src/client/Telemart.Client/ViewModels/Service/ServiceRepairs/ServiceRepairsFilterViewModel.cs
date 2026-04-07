using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs
{
    public sealed class ServiceRepairsFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private List<ServiceCenterDto> serviceCentersList;
        private List<WarehouseDto> warehousesList;

        public ServiceRepairsFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));

            SelectedStates = new List<object> { ServiceRepairState.New, ServiceRepairState.Confirmed };
        }

        public ObservableRangeCollection<ServiceCenterDto> ServiceCenters { get; } = new ObservableRangeCollection<ServiceCenterDto>();

        public ObservableRangeCollection<WarehouseDto> Warehouses { get; } = new ObservableRangeCollection<WarehouseDto>();

        public ObservableRangeCollection<ServiceRepairState> States { get; } = new ObservableRangeCollection<ServiceRepairState>();

        #region INPC

        public string RequestNumbers
        {
            get { return GetProperty(() => RequestNumbers); }
            set { SetProperty(() => RequestNumbers, value); }
        }

        public string InvoiceNumbers
        {
            get { return GetProperty(() => InvoiceNumbers); }
            set { SetProperty(() => InvoiceNumbers, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public List<object> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        public int? SelectedServiceCenterId
        {
            get { return GetProperty(() => SelectedServiceCenterId); }
            set { SetProperty(() => SelectedServiceCenterId, value); }
        }

        public int? SelectedWarehouseId
        {
            get { return GetProperty(() => SelectedWarehouseId); }
            set { SetProperty(() => SelectedWarehouseId, value); }
        }

        #endregion

        public string Error => string.Empty;

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ServiceRepairsFilterViewModel> builder)
        {
            builder.Property(x => x.RequestNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public Task RefreshAsync()
        {
            States.Clear();
            States.AddRange(Dictionaries.GetItems<ServiceRepairState>());

            return Task.WhenAll(
                RefreshServiceCentersAsync(),
                RefreshWarehousesAsync());
        }

        public void ResetFilterValues()
        {
            RequestNumbers = null;
            InvoiceNumbers = null;
            Name = null;
            SelectedServiceCenterId = null;
            SelectedStates = new List<object> { ServiceRepairState.New, ServiceRepairState.Confirmed };
            SelectedWarehouseId = Warehouses.First().Id;
        }

        public IFilteringItem GetFilteringItem()
        {
            return new RepairFilteringItem
            {
                Requests = RequestNumbers,
                Invoices = InvoiceNumbers,
                Name = Name,
                Warehouse = SelectedWarehouseId,
                States = SelectedStates?.Cast<ServiceRepairState>().Select(x => x.Id).ToList(),
                ServiceCenters = SelectedServiceCenterId.HasValue ? new List<int> { SelectedServiceCenterId.Value } : null
            };
        }

        private async Task RefreshServiceCentersAsync()
        {
            List<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(serviceCenters, serviceCentersList))
            {
                return;
            }

            ServiceCenters.Clear();

            serviceCentersList = serviceCenters;

            IOrderedEnumerable<ServiceCenterDto> serviceCenterItems = serviceCentersList
                .Where(x => x.Active)
                .OrderBy(x => x.Name);

            ServiceCenters.AddRange(serviceCenterItems);
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, warehousesList))
            {
                return;
            }

            Warehouses.Clear();

            warehousesList = warehouses;

            IOrderedEnumerable<WarehouseDto> warehouseItems = warehousesList
                .Where(x => WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id) && x.Active == 1 && x.TypeId == WarehouseKind.Service.Id)
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name);

            Warehouses.AddRange(warehouseItems);

            SelectedWarehouseId ??= Warehouses.First().Id;
        }
    }
}
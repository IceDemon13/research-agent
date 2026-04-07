using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class ServiceInvoicesFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private List<ServiceCenterDto> serviceCentersList;
        private List<WarehouseDto> warehousesList;

        public ServiceInvoicesFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));

            InitializeObservableCollections();
        }

        public ObservableRangeCollection<ComboBoxItem> ServiceCenters { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ServiceInvoiceState> ServiceInvoiceStates { get; } = new ObservableRangeCollection<ServiceInvoiceState>();

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        #region INPC

        public DateTime? ServiceInvoiceSendAfter
        {
            get { return GetProperty(() => ServiceInvoiceSendAfter); }
            set { SetProperty(() => ServiceInvoiceSendAfter, value); }
        }

        public DateTime? ServiceInvoiceSendBefore
        {
            get { return GetProperty(() => ServiceInvoiceSendBefore); }
            set { SetProperty(() => ServiceInvoiceSendBefore, value); }
        }

        public string InvoiceIds
        {
            get { return GetProperty(() => InvoiceIds); }
            set { SetProperty(() => InvoiceIds, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedServiceCenters
        {
            get { return GetProperty(() => SelectedServiceCenters); }
            set { SetProperty(() => SelectedServiceCenters, value); }
        }

        public ObservableCollection<ServiceInvoiceState> SelectedServiceInvoiceStates
        {
            get { return GetProperty(() => SelectedServiceInvoiceStates); }
            set { SetProperty(() => SelectedServiceInvoiceStates, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        #endregion

        public string Error => string.Empty;

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public Task RefreshAsync()
        {
            ServiceInvoiceStates.Clear();
            ServiceInvoiceStates.AddRange(Dictionaries.GetItems<ServiceInvoiceState>());

            return Task.WhenAll(
                RefreshServiceCentersAsync(),
                RefreshWarehousesAsync());
        }

        public void ResetFilterValues()
        {
            ServiceInvoiceSendAfter = null;
            ServiceInvoiceSendBefore = null;
            InvoiceIds = null;
            SelectedWarehouses = new ObservableCollection<ComboBoxItem>();
            SelectedServiceCenters = new ObservableCollection<ComboBoxItem>();
            SelectedServiceInvoiceStates = new ObservableCollection<ServiceInvoiceState>(GetDefaultStates());
        }

        public ServiceInvoiceFilteringItem GetServiceInvoiceFilteringItem()
        {
            ServiceInvoiceFilteringItem item = new ServiceInvoiceFilteringItem();

            item.ServiceInvoiceSendAfter = ServiceInvoiceSendAfter;
            item.ServiceInvoiceSendBefore = ServiceInvoiceSendBefore;
            item.InvoiceIds = InvoiceIds;
            item.Warehouses = SelectedWarehouses.Select(x => x.Id).ToList();
            item.ServiceCenters = SelectedServiceCenters.Select(x => x.Id).ToList();
            item.States = SelectedServiceInvoiceStates.Select(x => x.Id).ToList();

            return item;
        }

        private void InitializeObservableCollections()
        {
            SelectedServiceInvoiceStates = new ObservableCollection<ServiceInvoiceState>(GetDefaultStates());
            SelectedServiceCenters = new ObservableCollection<ComboBoxItem>();
            SelectedWarehouses = new ObservableCollection<ComboBoxItem>();
        }

        private static IEnumerable<ServiceInvoiceState> GetDefaultStates()
        {
            yield return ServiceInvoiceState.New;
            yield return ServiceInvoiceState.Closed;
            yield return ServiceInvoiceState.Sent;
        }

        private async Task RefreshServiceCentersAsync()
        {
            List<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(serviceCenters, serviceCentersList))
            {
                return;
            }

            ServiceCenters.Clear();
            serviceCentersList = serviceCenters.OrderBy(x => x.Name).ToList();

            ServiceCenters.AddRange(serviceCentersList.Where(x => x.Active).OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, warehousesList))
            {
                return;
            }

            Warehouses.Clear();
            warehousesList = warehouses.ToList();

            Warehouses.AddRange(warehousesList
                .Where(x => x.Active == 1 && x.TypeId == WarehouseKind.Service.Id && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(y => y.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name)));
        }
    }
}
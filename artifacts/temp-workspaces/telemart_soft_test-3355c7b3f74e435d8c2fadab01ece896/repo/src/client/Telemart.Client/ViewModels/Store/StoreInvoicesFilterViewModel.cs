using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class StoreInvoicesFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private List<ContractorDto> contractorsList;
        private List<WarehouseDto> warehousesList;

        public StoreInvoicesFilterViewModel(IDictionaries dictionaries, IWebClient webClient)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));

            Contractors = new ObservableRangeCollection<object>();
            Warehouses = new ObservableRangeCollection<ComboBoxItem>();
            InvoiceStateFilterViewItems = Dictionaries.GetItems<InvoiceState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableCollection();

            ResetFilterState();
        }

        public StoreInvoicesFilterViewModel()
        {
        }

        public ObservableRangeCollection<object> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public DateTime? InvoiceGetAfter
        {
            get { return GetProperty(() => InvoiceGetAfter); }
            set { SetProperty(() => InvoiceGetAfter, value); }
        }

        public DateTime? InvoiceGetBefore
        {
            get { return GetProperty(() => InvoiceGetBefore); }
            set { SetProperty(() => InvoiceGetBefore, value); }
        }

        public string InvoiceIds
        {
            get { return GetProperty(() => InvoiceIds); }
            set { SetProperty(() => InvoiceIds, value); }
        }

        public int InvoicesCount
        {
            get { return GetProperty(() => InvoicesCount); }
            set { SetProperty(() => InvoicesCount, value); }
        }

        public ObservableCollection<ComboBoxItem> InvoiceStateFilterViewItems
        {
            get { return GetProperty(() => InvoiceStateFilterViewItems); }
            private set { SetProperty(() => InvoiceStateFilterViewItems, value); }
        }

        public List<object> SelectedContractors
        {
            get { return GetProperty(() => SelectedContractors); }
            set { SetProperty(() => SelectedContractors, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedInvoiceStates
        {
            get { return GetProperty(() => SelectedInvoiceStates); }
            set { SetProperty(() => SelectedInvoiceStates, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public string Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<StoreInvoicesFilterViewModel> builder)
        {
            builder.Property(x => x.InvoiceIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public InvoiceFilteringItem GetInvoiceFilteringItem()
        {
            InvoiceFilteringItem item = new InvoiceFilteringItem
            {
                InvoiceGetAfter = InvoiceGetAfter,
                InvoiceGetBefore = InvoiceGetBefore,
                InvoicesIds = InvoiceIds,
                ContractorIds = SelectedContractors?.Select(x => ((ContractorDto)x).Id).ToArray(),
                InvoiceStatesIds = SelectedInvoiceStates?.Select(x => x.Id).ToArray(),
                WarehousesIds = SelectedWarehouses?.Select(x => x.Id).ToArray()
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(
                RefreshContractorsAsync(),
                RefreshWarehousesAsync());
        }

        public void ResetFilterState()
        {
            InvoiceGetAfter = null;
            InvoiceGetBefore = null;
            InvoiceIds = null;
            SelectedContractors = new List<object>();
            SelectedWarehouses = new ObservableCollection<ComboBoxItem>();
            SelectedInvoiceStates = GetDefaultInvoiceStateFilterItems().ToObservableCollection();

            IEnumerable<ComboBoxItem> GetDefaultInvoiceStateFilterItems()
            {
                yield return new ComboBoxItem(InvoiceState.Open.Id, InvoiceState.Open.Name);
                yield return new ComboBoxItem(InvoiceState.Closed.Id, InvoiceState.Closed.Name);
                yield return new ComboBoxItem(InvoiceState.Arrived.Id, InvoiceState.Arrived.Name);
            }
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, warehousesList))
            {
                return;
            }

            HashSet<int> selectedWarehouseIds = new HashSet<int>(SelectedWarehouses.Select(x => x.Id));

            Warehouses.Clear();
            warehousesList = warehouses;
            Warehouses.AddRange(warehousesList
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id) && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Assembly.Id))
                .OrderByDescending(y => y.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name)));
            SelectedWarehouses = new ObservableCollection<ComboBoxItem>(Warehouses.Where(x => selectedWarehouseIds.Contains(x.Id)));
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractors, contractorsList))
            {
                return;
            }

            List<int> selectedContractorIds = SelectedContractors?.Select(x => ((ContractorDto)x).Id).ToList();

            Contractors.Clear();
            contractorsList = contractors;
            Contractors.AddRange(contractorsList.Where(x => x.Active && x.IsFolder == false && x.IsSupplier).OrderBy(x => x.Name));

            if (selectedContractorIds != null)
            {
                SelectedContractors = Contractors.Where(x => selectedContractorIds.Contains(((ContractorDto)x).Id)).ToList();
            }
        }
    }
}
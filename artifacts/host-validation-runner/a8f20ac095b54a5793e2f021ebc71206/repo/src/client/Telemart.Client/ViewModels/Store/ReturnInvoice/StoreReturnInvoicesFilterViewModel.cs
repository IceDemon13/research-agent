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

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class StoreReturnInvoicesFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private List<ContractorDto> contractorsList;
        private List<WarehouseDto> warehousesList;

        public StoreReturnInvoicesFilterViewModel(IDictionaries dictionaries, IWebClient webClient)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));

            Contractors = new ObservableRangeCollection<object>();
            Warehouses = new ObservableRangeCollection<ComboBoxItem>();

            ReturnInvoiceStateFilterViewItems = Dictionaries
                .GetItems<ReturnInvoiceState>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();

            CancelProductCommand = new DelegateCommand(CleanProduct);
            ResetFilterState();
        }

        public StoreReturnInvoicesFilterViewModel()
        {
        }

        public IDelegateCommand CancelProductCommand { get; }

        public string ReturnInvoiceIds
        {
            get { return GetProperty(() => ReturnInvoiceIds); }
            set { SetProperty(() => ReturnInvoiceIds, value); }
        }

        public string InvoiceIds
        {
            get { return GetProperty(() => InvoiceIds); }
            set { SetProperty(() => InvoiceIds, value); }
        }

        public string WarehouseIds
        {
            get { return GetProperty(() => WarehouseIds); }
            set { SetProperty(() => WarehouseIds, value); }
        }

        public DateTime? ReturnedAfter
        {
            get { return GetProperty(() => ReturnedAfter); }
            set { SetProperty(() => ReturnedAfter, value); }
        }

        public DateTime? ReturnedBefore
        {
            get { return GetProperty(() => ReturnedBefore); }
            set { SetProperty(() => ReturnedBefore, value); }
        }

        public DateTime? CreatedAfter
        {
            get { return GetProperty(() => CreatedAfter); }
            set { SetProperty(() => CreatedAfter, value); }
        }

        public DateTime? CreatedBefore
        {
            get { return GetProperty(() => CreatedBefore); }
            set { SetProperty(() => CreatedBefore, value); }
        }

        public int ReturnInvoicesCount
        {
            get { return GetProperty(() => ReturnInvoicesCount); }
            set { SetProperty(() => ReturnInvoicesCount, value); }
        }

        public bool? ReadyPack
        {
            get { return GetProperty(() => ReadyPack); }
            set { SetProperty(() => ReadyPack, value); }
        }

        public ObservableCollection<ComboBoxItem> ReturnInvoiceStateFilterViewItems
        {
            get { return GetProperty(() => ReturnInvoiceStateFilterViewItems); }
            private set { SetProperty(() => ReturnInvoiceStateFilterViewItems, value); }
        }

        public List<object> SelectedContractors
        {
            get { return GetProperty(() => SelectedContractors); }
            set { SetProperty(() => SelectedContractors, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedReturnInvoiceStates
        {
            get { return GetProperty(() => SelectedReturnInvoiceStates); }
            set { SetProperty(() => SelectedReturnInvoiceStates, value); }
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

        public ObservableRangeCollection<object> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<StoreReturnInvoicesFilterViewModel> builder)
        {
            builder.Property(x => x.ReturnInvoiceIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.InvoiceIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public ReturnInvoiceFilteringItem GetInvoiceFilteringItem()
        {
            ReturnInvoiceFilteringItem item = new ReturnInvoiceFilteringItem
            {
                ReturnedAfter = ReturnedAfter,
                ReturnedBefore = ReturnedBefore,
                CreatedAfter = CreatedAfter,
                CreatedBefore = CreatedBefore,
                ReadyPack = ReadyPack,
                ReturnInvoicesIds = ReturnInvoiceIds,
                InvoiceIds = InvoiceIds,
                ProductId = ProductId,
                ProductName = ProductName,
                ContractorIds = SelectedContractors?.Select(x => ((ContractorDto)x).Id).ToList(),
                ReturnInvoiceStatesIds = SelectedReturnInvoiceStates?.Select(x => x.Id).ToList(),
                WarehouseIds = SelectedWarehouses?.Select(x => x.Id).ToList()
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
            ReturnedAfter = null;
            ReturnedAfter = null;
            CreatedAfter = null;
            CreatedBefore = null;
            ReadyPack = null;
            ReturnInvoiceIds = null;
            InvoiceIds = null;
            ProductName = null;
            ProductId = null;
            ProductName = null;
            SelectedContractors = new List<object>();
            SelectedWarehouses = new ObservableCollection<ComboBoxItem>();
            SelectedReturnInvoiceStates = GetDefaultInvoiceStateFilterItems().ToObservableCollection();

            IEnumerable<ComboBoxItem> GetDefaultInvoiceStateFilterItems()
            {
                yield return new ComboBoxItem(ReturnInvoiceState.New.Id, ReturnInvoiceState.New.Name);
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
                .Where(x => x.Active == 1 && x.TypeId == WarehouseKind.Main.Id && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
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

        private void CleanProduct()
        {
            ProductId = null;
            ProductName = null;
        }
    }
}
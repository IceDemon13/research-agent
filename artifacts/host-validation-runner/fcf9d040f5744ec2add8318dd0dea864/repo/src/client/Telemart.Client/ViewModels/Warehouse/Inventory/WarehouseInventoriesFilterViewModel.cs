using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    public class WarehouseInventoriesFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<WarehouseDto> warehousesList;

        public WarehouseInventoriesFilterViewModel(IWebClient webClient)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        public WarehouseInventoriesFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        #endregion

        public DateTime? DateAfter
        {
            get { return GetProperty(() => DateAfter); }
            set { SetProperty(() => DateAfter, value); }
        }

        public DateTime? DateBefore
        {
            get { return GetProperty(() => DateBefore); }
            set { SetProperty(() => DateBefore, value); }
        }

        public string InventoryNumbers
        {
            get { return GetProperty(() => InventoryNumbers); }
            set { SetProperty(() => InventoryNumbers, value); }
        }

        public List<object> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<WarehouseInventoriesFilterViewModel> builder)
        {
            builder.Property(x => x.InventoryNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public InventoryFilteringItem GetInventoryFilteringItem()
        {
            InventoryFilteringItem item = new InventoryFilteringItem
            {
                InventoryNumbers = InventoryNumbers,
                DateAfter = DateAfter,
                DateBefore = DateBefore,
                Warehouses = SelectedWarehouses?.Cast<ComboBoxItem>().Select(x => x.Id).ToList()
            };
            return item;
        }

        public Task RefreshAsync()
        {
            return RefreshWarehousesAsync();
        }

        public void ResetFilterValues()
        {
            DateAfter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, warehousesList))
            {
                return;
            }

            warehouses = warehouses
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .ToList();

            Warehouses.Clear();
            warehousesList = warehouses;

            Warehouses.AddRange(warehouses.Select(x => new ComboBoxItem(x.Id, x.Name)));
        }
    }
}
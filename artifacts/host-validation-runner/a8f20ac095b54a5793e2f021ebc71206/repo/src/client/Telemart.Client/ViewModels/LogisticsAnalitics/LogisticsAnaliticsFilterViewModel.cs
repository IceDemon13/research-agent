using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.LogisticsAnalitics
{
    public sealed class LogisticsAnaliticsFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private List<WarehouseDto> warehousesList;

        public LogisticsAnaliticsFilterViewModel(IWebClient webClient)
        {
            WebClient = webClient;
        }

        public string Error => string.Empty;

        private IWebClient WebClient { get; }

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public int? SelectedWarehouseId
        {
            get { return GetProperty(() => SelectedWarehouseId); }
            set { SetProperty(() => SelectedWarehouseId, value); }
        }

        public bool ExpiredOnly
        {
            get { return GetProperty(() => ExpiredOnly); }
            set { SetProperty(() => ExpiredOnly, value); }
        }

        public LogisticsAnaliticsFilteringItem GetFilteringItem()
        {
            LogisticsAnaliticsFilteringItem item = new LogisticsAnaliticsFilteringItem
            {
                WarehouseIds = SelectedWarehouseId.HasValue ? new[] { SelectedWarehouseId.Value } : Array.Empty<int>(),
                ExpiredOnly = ExpiredOnly
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return RefreshWarehousesAsync();
        }

        public void ResetFilterValues()
        {
            SelectedWarehouseId = null;
            ExpiredOnly = false;
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
            ExpiredOnly = false;

            Warehouses.AddRange(warehousesList
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderBy(y => y.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name)));
        }
    }
}
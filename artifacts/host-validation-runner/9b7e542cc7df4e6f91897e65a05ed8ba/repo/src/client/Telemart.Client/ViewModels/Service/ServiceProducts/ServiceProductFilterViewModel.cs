using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Validation;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<WarehouseDto> warehousesList;

        public ServiceProductFilterViewModel(IWebClient webClient)
        {
            WebClient = webClient;
        }

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public string ServiceProductNumbers
        {
            get { return GetProperty(() => ServiceProductNumbers); }
            set { SetProperty(() => ServiceProductNumbers, value); }
        }

        public string ServiceRequestIds
        {
            get { return GetProperty(() => ServiceRequestIds); }
            set { SetProperty(() => ServiceRequestIds, value); }
        }

        public string Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public List<object> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        public List<object> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        public DateTime? CreatedOnBefore
        {
            get { return GetProperty(() => CreatedOnBefore); }
            set { SetProperty(() => CreatedOnBefore, value); }
        }

        public DateTime? CreatedOnAfter
        {
            get { return GetProperty(() => CreatedOnAfter); }
            set { SetProperty(() => CreatedOnAfter, value); }
        }

        public DateTime? CompletedOnAfter
        {
            get { return GetProperty(() => CompletedOnAfter); }
            set { SetProperty(() => CompletedOnAfter, value); }
        }

        public DateTime? CompletedOnBefore
        {
            get { return GetProperty(() => CompletedOnBefore); }
            set { SetProperty(() => CompletedOnBefore, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ServiceProductFilterViewModel> builder)
        {
            builder.Property(x => x.ServiceProductNumbers).NumberArray();

            builder.Property(x => x.ServiceRequestIds).NumberArray();
        }

        public ServiceProductFilteringItem GetFilteringItem()
        {
            ServiceProductFilteringItem item = new ServiceProductFilteringItem
            {
                ServiceProductNumbers = ServiceProductNumbers,
                ServiceRequestIds = ServiceRequestIds,
                Product = Product,
                SerialNumber = SerialNumber,
                CreatedOnBefore = CreatedOnBefore,
                CreatedOnAfter = CreatedOnAfter,
                CompletedOnAfter = CompletedOnAfter,
                CompletedOnBefore = CompletedOnBefore,
                WarehouseIds = SelectedWarehouses?.Cast<int>().ToList(),
                States = SelectedStatuses?.Cast<int>().ToList()
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return RefreshWarehousesAsync();
        }

        public void ResetFilterValues()
        {
            ServiceProductNumbers = null;
            ServiceRequestIds = null;
            Product = null;
            SerialNumber = null;
            CreatedOnBefore = null;
            CreatedOnAfter = null;
            CompletedOnBefore = null;
            CompletedOnAfter = null;
            SelectedWarehouses = new List<object>();
            SelectedStatuses = new List<object>
            {
                ServiceProductState.New.Id,
                ServiceProductState.OnUtilization.Id,
                ServiceProductState.TransferToSupplier.Id,
                ServiceProductState.RemovingFromRegister.Id,
                ServiceProductState.OnReapir.Id
            };
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

            int[] needWarehouseTypeIds = { WarehouseKind.Assembly.Id, WarehouseKind.Service.Id };

            Warehouses.AddRange(warehousesList
                .Where(x => x.Active == 1 && needWarehouseTypeIds.Contains(x.TypeId) && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderBy(y => y.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name)));
        }
    }
}
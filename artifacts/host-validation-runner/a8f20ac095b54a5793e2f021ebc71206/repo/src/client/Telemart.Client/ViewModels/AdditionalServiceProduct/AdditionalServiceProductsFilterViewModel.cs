using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public class AdditionalServiceProductsFilterViewModel : TelemartViewItemBase
    {
        public AdditionalServiceProductsFilterViewModel()
        {
            Reset();
        }

        public string Ids
        {
            get { return GetProperty(() => Ids); }
            set { SetProperty(() => Ids, value); }
        }

        public string OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public List<object> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        public List<object> SelectedOrderStates
        {
            get { return GetProperty(() => SelectedOrderStates); }
            set { SetProperty(() => SelectedOrderStates, value); }
        }

        public DateTime? OrderDeliveryTimeToAfter
        {
            get { return GetProperty(() => OrderDeliveryTimeToAfter); }
            set { SetProperty(() => OrderDeliveryTimeToAfter, value); }
        }

        public DateTime? OrderDeliveryTimeToBefore
        {
            get { return GetProperty(() => OrderDeliveryTimeToBefore); }
            set { SetProperty(() => OrderDeliveryTimeToBefore, value); }
        }

        public List<object> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        public List<object> SelectedOrderWarehouses
        {
            get { return GetProperty(() => SelectedOrderWarehouses); }
            set { SetProperty(() => SelectedOrderWarehouses, value); }
        }

        public List<object> SelectedEmployees
        {
            get { return GetProperty(() => SelectedEmployees); }
            set { SetProperty(() => SelectedEmployees, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AdditionalServiceProductsFilterViewModel> builder)
        {
            builder.Property(x => x.Ids)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.OrderIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public IFilteringItem GetFilteringItem()
        {
            IEnumerable<int> warehouseIds = SelectedWarehouses?.Cast<ComboBoxItem>().Select(x => x.Id);
            IEnumerable<int> orderWarehouseIds = SelectedOrderWarehouses?.Cast<ComboBoxItem>().Select(x => x.Id);
            IEnumerable<int> employeeIds = SelectedEmployees?.Cast<ComboBoxItem>().Select(x => x.Id).ToArray();
            IEnumerable<int> stateIds = SelectedStates?.Cast<AdditionalServiceProductState>().Select(x => x.Id);
            IEnumerable<int> orderStateIds = SelectedOrderStates?.Cast<OrderStatus>().Select(x => x.Id);

            return new AdditionalServiceProductsFilteringItem()
            {
                Ids = Ids,
                OrderIds = OrderIds,
                StateIds = stateIds is null ? null : string.Join(",", stateIds),
                OrderStateIds = orderStateIds is null ? null : string.Join(",", orderStateIds),
                WarehouseIds = warehouseIds is null ? null : string.Join(",", warehouseIds),
                OrderWarehouseIds = orderWarehouseIds is null ? null : string.Join(",", orderWarehouseIds),
                EmployeeIds = employeeIds is null ? null : string.Join(",", employeeIds),
                OrderDeliveryTimeToAfter = OrderDeliveryTimeToAfter,
                OrderDeliveryTimeToBefore = OrderDeliveryTimeToBefore
            };
        }

        public void Reset()
        {
            Ids = null;
            OrderIds = null;
            SelectedWarehouses = null;
            SelectedOrderWarehouses = null;
            OrderDeliveryTimeToAfter = null;
            OrderDeliveryTimeToBefore = null;
            SelectedEmployees = null;
            SelectedStates = new List<object> { AdditionalServiceProductState.Warehouse, AdditionalServiceProductState.Waiting, AdditionalServiceProductState.Doing };
            SelectedOrderStates = new List<object> { OrderStatus.Received, OrderStatus.Confirmed };
        }
    }
}
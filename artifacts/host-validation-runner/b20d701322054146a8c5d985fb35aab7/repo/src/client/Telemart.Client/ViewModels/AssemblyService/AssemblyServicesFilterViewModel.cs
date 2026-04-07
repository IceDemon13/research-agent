using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public sealed class AssemblyServicesFilterViewModel : TelemartViewItemBase
    {
        public AssemblyServicesFilterViewModel()
        {
            CancelAssemblyServiceProductCommand = new DelegateCommand(CancelAssemblyServiceProduct);
            CancelProductCommand = new DelegateCommand(CancelProduct);

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

        public DateTime? AssemblyDateBefore
        {
            get { return GetProperty(() => AssemblyDateBefore); }
            set { SetProperty(() => AssemblyDateBefore, value); }
        }

        public DateTime? AssemblyDateAfter
        {
            get { return GetProperty(() => AssemblyDateAfter); }
            set { SetProperty(() => AssemblyDateAfter, value); }
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

        public string AssemblyServiceProductName
        {
            get { return GetProperty(() => AssemblyServiceProductName); }
            set { SetProperty(() => AssemblyServiceProductName, value); }
        }

        public int? AssemblyServiceProductId
        {
            get { return GetProperty(() => AssemblyServiceProductId); }
            set { SetProperty(() => AssemblyServiceProductId, value); }
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

        public IDelegateCommand CancelAssemblyServiceProductCommand { get; }

        public IDelegateCommand CancelProductCommand { get; }

        public static void BuildMetadata(MetadataBuilder<AssemblyServicesFilterViewModel> builder)
        {
            builder.Property(x => x.Ids)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.OrderIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public IFilteringItem GetFilteringItem()
        {
            IEnumerable<int> stateIds = SelectedStates?.Cast<AssemblyServiceState>().Select(x => x.Id);

            return new AssemblyServicesFilteringItem()
            {
                Ids = Ids,
                StateIds = stateIds is null ? null : string.Join(",", stateIds),
                OrderIds = OrderIds,
                OrderDeliveryTimeToBefore = OrderDeliveryTimeToBefore,
                AssemblyDateBefore = AssemblyDateBefore,
                AssemblyDateAfter = AssemblyDateAfter,
                OrderDeliveryTimeToAfter = OrderDeliveryTimeToAfter,
                AssemblyServiceProductId = AssemblyServiceProductId,
                ProductId = ProductId
            };
        }

        public void Reset()
        {
            Ids = null;
            OrderIds = null;
            AssemblyDateBefore = null;
            AssemblyDateAfter = null;
            OrderDeliveryTimeToAfter = null;
            OrderDeliveryTimeToBefore = null;
            SelectedStates = new List<object> { AssemblyServiceState.Waiting, AssemblyServiceState.Assembling, AssemblyServiceState.Testing, AssemblyServiceState.Assembled, AssemblyServiceState.Warehouse };
            AssemblyServiceProductId = null;
            AssemblyServiceProductName = null;
            ProductName = null;
            ProductId = null;
        }

        private void CancelAssemblyServiceProduct()
        {
            AssemblyServiceProductName = null;
            AssemblyServiceProductId = null;
        }

        private void CancelProduct()
        {
            ProductName = null;
            ProductId = null;
        }
    }
}